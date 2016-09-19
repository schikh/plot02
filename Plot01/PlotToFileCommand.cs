using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using BatchPlot;
using BatchPlot.Configuration;
using BatchPlot.Extensions;
using BatchPlot.Services;

//    C:\Users\adn534>print /d:\\U90PHIMPT005\NA83  "C:\Test\plot\Plot01\Scripts\dump2.bin"
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr" /id 184128H /r 500 /z West /c "MAP" /e "BT,MT,EP,BP,MP" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /st "1629628-29519681" /t 7 /n 1 /u ADN534 /isolate
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr" /id 079145E /r 500 /z West /c "MAP" /e "BT" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /imp /st "1629628-29519681" /t 7 /n 1 /u ADN534   /isolate
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr" /id 079145E /r 500 /z West /c "MAP" /e "BT" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /p "Canon C5235 - MERCK NAM IT - BSM Reseaux" /imp /st "1629628-29519681" /t 7 /n 1 /u ADN534   /isolate
//                                                                               /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr" /id 184128H /r 500 /z Est /c "MAP" /e "BT,MT,EP,BP,MP" /imp  /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /st "1629628-29519681" /t 7 /n 1 /u ADN534 /isolate
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\plot\Plot01\Files\F185128.DWG" /s "C:\Test\Plot\Plot01\Scripts\PlotDwg.scr" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /isolate
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\plot\Plot01\Files\F185128.DWG" /s "C:\Test\Plot\Plot01\Scripts\PlotDwg.scr" /p "Canon C5235 - MERCK NAM IT - BSM Reseaux" /d /isolate
//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr" /id 079145E /r 500 /z West /c "MAP" /e "BT" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /p "Canon C5235 - MERCK NAM IT - BSM Reseaux" /d /imp /st "1629628-29519681" /t 7 /n 1 /u ADN534 /isolate
[assembly: CommandClass(typeof(PlotToFileCommand))]
namespace BatchPlot
{
	public class PlotToFileCommand : IExtensionApplication
	{
		private string _tempFolder;
		private readonly Document _document = Application.DocumentManager.MdiActiveDocument;
		private PlotParameters _plotParameters;

		public void Initialize()
		{
			Logger.Setup();
		}

		public void Terminate()
		{
		}

		[CommandMethod("PlotPlanchette")]
		public void PlotPlanchette()
		{
			try
			{
				_document.Database.Insunits = UnitsValue.Millimeters;
				_tempFolder = Helper.CreateTempFolder();

				var args = Environment.GetCommandLineArgs();
				Helper.Log("ARGUMENTS: " + string.Join(" ", args.Skip(1)));
				_plotParameters = new PlotParameters(args);

				//var filePaths = Directory.GetFiles(@"C:\Test\Plot\Plot01\Files2").Take(2);
				var energies = ExtendEnergiesSelection(_plotParameters.Energies);
				_plotParameters.EnergyDescription = energies;
				var serverFilePaths = GetServerFilePaths(_plotParameters.Category, energies);
				var filePaths = ImportServerFiles(serverFilePaths).ToArray();

				OpenFiles(filePaths);

				if (_plotParameters.Impetrant)
				{
					using (var tr = _document.Database.TransactionManager.StartTransaction())
					{
						ApplyImpetrantStyle(tr);
						tr.Commit();
					}
				}

				CreateAndConfigureLayout();

				AddPlotCartridge(_plotParameters.CartridgeTemplate, _plotParameters.PlotCartridgePosition);

				SetPageView();

				PlotExtents();

				SaveDwg(@"C:\Test\Plot\Plot01\Scripts\dump2.dwg");

				//DeleteImportedFiles();

				Helper.Log("PLOT SUCCESSFUL");
			}
			catch (System.Exception ex)
			{
				Helper.Log("PLOT ERROR");
				Helper.Log(ex);
				Environment.ExitCode = 999;
			}
		}

		[CommandMethod("PlotDwg")]
		public void PlotDwg()
		{
			try
			{
				_document.Database.Insunits = UnitsValue.Millimeters;

				var args = Environment.GetCommandLineArgs();
				Helper.Log("ARGUMENTS: " + string.Join(" ", args.Skip(1)));
				_plotParameters = new PlotParameters(args);

				using (var tr = _document.Database.TransactionManager.StartTransaction())
				{
					using (var layout = GetPlotLayout(tr))
					{
						SetPlotSettings(layout);
					}
					tr.Commit();
				}

				PlotExtents();

				Helper.Log("PLOT SUCCESSFUL");
			}
			catch (System.Exception ex)
			{
				Helper.Log("PLOT ERROR");
				Helper.Log(ex);
				Environment.ExitCode = 999;
			}
		}

		private List<string> GetServerFilePaths(string category, IEnumerable<string> energies)
		{
			//              + "AND energie NOT IN ('SYS','COMM','ELEC','GAZ','IC','RE','TPCD','TPDV','TPMD','TPRC') "
			var da = new DataAccessService(PlotConfiguration.Config.ConnectionString);
			var query = string.Format("SELECT path, fileName "
				+ "FROM dessin "
				+ "WHERE serveur = '{0}' "
				+ "AND categorie = '{5}' "
				+ "AND energie IN('{6}') "
				+ "AND path NOT LIKE '%#%' "
				+ "AND xmax >= {1} AND xmin <= {2} AND ymax >= {3} AND ymin <= {4} ",
				_plotParameters.FileServerName,
				_plotParameters.DrawingExtend.MinPoint.X,
				_plotParameters.DrawingExtend.MaxPoint.X,
				_plotParameters.DrawingExtend.MinPoint.Y,
				_plotParameters.DrawingExtend.MaxPoint.Y,
				category,
				string.Join("','", energies));
			var list = da.IterateOverReader(query, x => Path.Combine(x.GetString(0), x.GetString(1))).ToList();
			Helper.Log("Query table DESSIN: " + query);
			Helper.Log("Number OF file found: " + list.Count);
			return list;
		}

		private IEnumerable<string> ExtendEnergiesSelection(IEnumerable<string> energies)
		{
			/*
			'NETGIS', null    TOPO, DEFAULT_ENERGIE
			'CAPGIS', null    TOPO, DEFAULT_ENERGIE
			'RS',     null,   TOPO, IC
			'OX',     null,   OX
			'DZ',     null,   DZ
			'NETGIS', '*'     TOPO, *
			'*',      '*'     TOPO, *
			*/

			if (energies.Count() == 1)
			{
				switch (energies.First())
				{
					case "NETGIS":
					case "CAPGIS":
						energies = new[] { "TOPO", "DEFAULT_ENERGIE" };
						break;
					case "RS":
						energies = new[] { "TOPO", "IC" };
						break;
					case "OX":
					case "DZ":
						break;
					default:
						energies = energies.Concat(new[] { "TOPO" }).ToArray();
						break;
				}
			}
			else
			{
				energies = energies.Concat(new[] { "TOPO" }).ToArray();
			}

			var da = new DataAccessService(PlotConfiguration.Config.ConnectionString);
			var query = string.Format(
                  "SELECT DISTINCT COALESCE(a.ENERGY, b.TITLEID) AS ENERGY, COALESCE(b.RANK, 50) "
				+ "FROM PLOTSRV_REQENERGIEGROUP a "
				+ "FULL OUTER JOIN PLOTMAPLAYERTITLE b "
				+ "ON a.ENERGY = b.TITLEID "
				+ "WHERE a.GROUPNAME IN('{0}') " 
				+ "OR b.TITLEID IN('{0}') "
				+ "ORDER BY 2, 1 ",
				string.Join("','", energies));
			var list = da.IterateOverReader(query, x => x.GetString(0));
			list = list.Concat(energies).Distinct().OrderBy(x => x);
			return list;
		}

		private IEnumerable<string> ImportServerFiles(IEnumerable<string> serverFilePaths)
		{
			foreach (var serverFilePath in serverFilePaths)
			{
				if (File.Exists(serverFilePath))
				{
					var localFilePath = Helper.GetLocalFilePath(serverFilePath, _tempFolder);
					File.Copy(serverFilePath, localFilePath, true);
					yield return localFilePath;
				}
				else
				{
					Helper.Log("FILE NOT FOUND {0}", serverFilePath);
				}
			}
		}

		private void OpenFiles(IEnumerable<string> filePaths)
		{
			using (var tr = _document.Database.TransactionManager.StartTransaction())
			using (var bt = (BlockTable)_document.Database.BlockTableId.GetObject(OpenMode.ForRead))
			using (var btr = (BlockTableRecord)bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite))
			{
                var c = filePaths.Count();
                var i = 0;
                foreach (var filePath in filePaths)
				{
					Helper.Log("OPEN FILE {1}/{2}   {0}", filePath, ++i, c);
					var br = OpenFile(filePath);
					btr.AppendEntity(br);
					tr.AddNewlyCreatedDBObject(br, true);
				}
				tr.Commit();
			}
		}

		private BlockReference OpenFile(string filePath)
		{
			using (var db = new Database(false, true))
			{
				db.ReadDwgFile(filePath, FileShare.Read, false, "");
				DeleteNotNeededLayers(db);
				var blockName = Path.GetFileNameWithoutExtension(filePath);
				var id = _document.Database.Insert(blockName, db, true);
				return new BlockReference(Point3d.Origin, id);
			}
		}

		private void AddPlotCartridge(string templateFilePath, Point3d position)
		{
			using (var tr = _document.Database.TransactionManager.StartTransaction())
			{
				using (var layout = GetPlotLayout(tr))
				using (var btr = (BlockTableRecord) tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
				{
					ObjectId blockId;
					using (var db = new Database(false, true))
					{
						db.ReadDwgFile(templateFilePath, FileShare.Read, true, "");
						var blockName = Path.GetFileNameWithoutExtension(templateFilePath);
						blockId = _document.Database.Insert(blockName, db, true);
					}

					var values = GetCartridgeInfo(tr);

					using (var blockDefinition = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead))
					using (var br = new BlockReference(position, blockId))
					{
						btr.AppendEntity(br);
						tr.AddNewlyCreatedDBObject(br, true);
						blockDefinition.CopyAttributeDefinition(br, values);
					}
                    tr.Commit();
                }
			}
		}

		private void AddStamp(Transaction tr, Layout layout)
		{
			using (var btr = (BlockTableRecord) tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
			using (var text = new DBText())
			{
				text.SetDatabaseDefaults();
				text.TextString = _plotParameters.Stamp;
				text.HorizontalMode = TextHorizontalMode.TextLeft;
				text.VerticalMode = TextVerticalMode.TextTop;
				text.AlignmentPoint = _plotParameters.StampPosition;
				text.Rotation = Math.PI / 2;
				text.Height = 4;
				text.AdjustAlignment(_document.Database);
				btr.AppendEntity(text);
				tr.AddNewlyCreatedDBObject(text, true);
			}
		}

		private void AddDrawingBorders(Transaction tr, Layout layout)
		{
			using (var btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
			{
				var x = PlotConfiguration.Config.ExternalBorderWidth + PlotConfiguration.Config.InternalBorderWidth;
				var y = PlotConfiguration.Config.ExternalBorderWidth + PlotConfiguration.Config.InternalBorderWidth;
				var width = PlotConfiguration.Config.DrawingSize.Width * _plotParameters.Scale;
				var height = PlotConfiguration.Config.DrawingSize.Height * _plotParameters.Scale;
				
				var rectangle = CreateRectangle(x, y, height, width);
				rectangle.ColorIndex = 9;
				btr.AppendEntity(rectangle);
				tr.AddNewlyCreatedDBObject(rectangle, true);

				rectangle = InflateRectangle(rectangle, PlotConfiguration.Config.InternalBorderWidth);
				btr.AppendEntity(rectangle);
				tr.AddNewlyCreatedDBObject(rectangle, true);

				rectangle = InflateRectangle(rectangle, PlotConfiguration.Config.ExternalBorderWidth);
				btr.AppendEntity(rectangle);
				tr.AddNewlyCreatedDBObject(rectangle, true);
			}
		}

		private Polyline InflateRectangle(Polyline rectangle, int size)
		{
			var line = new Polyline();
			line.AddVertexAt(0, rectangle.GetPoint2dAt(0).Add(new Vector2d(-size, -size)), 0, 0, 0);
			line.AddVertexAt(1, rectangle.GetPoint2dAt(1).Add(new Vector2d(-size, +size)), 0, 0, 0);
			line.AddVertexAt(2, rectangle.GetPoint2dAt(2).Add(new Vector2d(+size, +size)), 0, 0, 0);
			line.AddVertexAt(3, rectangle.GetPoint2dAt(3).Add(new Vector2d(+size, -size)), 0, 0, 0);
			line.AddVertexAt(4, rectangle.GetPoint2dAt(4).Add(new Vector2d(-size, -size)), 0, 0, 0);
			return line;
		}

		private Polyline CreateRectangle(double x, double y, double height, double width)
		{
			var line = new Polyline();
			line.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
			line.AddVertexAt(1, new Point2d(x, y + height), 0, 0, 0);
			line.AddVertexAt(2, new Point2d(x + width, y + height), 0, 0, 0);
			line.AddVertexAt(3, new Point2d(x + width, y), 0, 0, 0);
			line.AddVertexAt(4, new Point2d(x, y), 0, 0, 0);
			return line;
		}

		private IEnumerable<T> QueryEntities<T>(Transaction tr, IEnumerable<string> layers, Extents2d extend) where T : Entity
		{
			var bt = (IEnumerable<dynamic>)(dynamic)_document.Database.BlockTableId;
			var list = bt.SelectMany(x => (IEnumerable<dynamic>)x)
				.Where(x => x.IsKindOf(typeof(T)))
				.Select(x => (T)tr.GetObject(x, OpenMode.ForRead));
			if (layers != null)
			{
				list = list.Where(x => layers.Contains(x.Layer));
			}
			list = list.Where(x => x.Bounds.HasValue
				&& x.Bounds.Value.MaxPoint.X >= extend.MinPoint.X
				&& x.Bounds.Value.MinPoint.X <= extend.MaxPoint.X
				&& x.Bounds.Value.MaxPoint.Y >= extend.MinPoint.Y
				&& x.Bounds.Value.MinPoint.Y <= extend.MaxPoint.Y)
				.ToArray();
			return list;
		}

		private IEnumerable<Entity> QueryEntities(Transaction tr, string layerRegexFilter, Extents2d extend)
		{
			var bt = (IEnumerable<dynamic>)(dynamic)_document.Database.BlockTableId;
			var list = bt.SelectMany(x => (IEnumerable<dynamic>)x)
				.Select(x => (Entity)tr.GetObject(x, OpenMode.ForRead));
			if (layerRegexFilter != null)
			{
				var regex = new Regex(layerRegexFilter, RegexOptions.IgnoreCase);
				list = list.Where(x => regex.IsMatch(x.Layer));
			}
			list = list.Where(x => x.Bounds.HasValue
				&& x.Bounds.Value.MaxPoint.X >= extend.MinPoint.X
				&& x.Bounds.Value.MinPoint.X <= extend.MaxPoint.X
				&& x.Bounds.Value.MaxPoint.Y >= extend.MinPoint.Y
				&& x.Bounds.Value.MinPoint.Y <= extend.MaxPoint.Y)
				.ToArray();
			return list;
		}

		private Dictionary<string, string> GetCartridgeInfo(Transaction tr)
		{
			var values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			GetStandardCartridgeInfo(tr)
				.Concat(GetStreetsList(tr))
				.Concat(GetCommunesList(tr))
				.Concat(GetModificationsList(tr))
				.Concat(GetPlanchetteIdsList())
				.ToList()
				.ForEach(x => values.Add(x.Key, x.Value));
			return values;
		}

		private Dictionary<string, string> GetStandardCartridgeInfo(Transaction tr)
		{
			var values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			values.Add("ECH", string.Format("1/{0}", _plotParameters.Resolution));
			values.Add("DAT", DateTime.Now.ToString("dd.MM.yy (HH:mm)"));
			values.Add("NUM", _plotParameters.PlanchetteId);
			values.Add("DES", _plotParameters.UserId);
			values.Add("RES", (_plotParameters.Energies.Length == 1 ? "Réseau: " : "Réseaux: ")
				+ string.Join(", ", _plotParameters.Energies));
			values.Add("OBJ1", "Situation des installations");
			return values;
		}

		private Dictionary<string, string> GetStreetsList(Transaction tr)
		{
			var values = new Dictionary<string, string>();
			Enumerable.Range(1, 5).ToList().ForEach(x => values.Add("RUE" + x, ""));
			var i = 0;
			var layers = new[] { "BR11", "BT02", "BW03", "RUE", "W0989" };
			var list = QueryEntities<DBText>(tr, layers, _plotParameters.DrawingExtend);
			list.Select(x => x.TextString)
				.GroupBy(x => x)
				.OrderByDescending(x => x.Count())
				.Select(x => x.Key)
				.Take(15)
				.OrderBy(x => x)
				.GroupListItems(5)
				.ToList()
				.ForEach(x => values["RUE" + ++i] = x);
			return values;
		}

		private Dictionary<string, string> GetCommunesList(Transaction tr)
		{
			var values = new Dictionary<string, string>();
			values.Add("COM", "");
			values.Add("COM2", "");
			var i = 0;
			var layers = new[] { "BL02", "COMMUNES", "COMMUNE", "W0980" };
			var list = QueryEntities<DBText>(tr, layers, _plotParameters.DrawingExtend);
			list.Select(x => x.TextString)
				.GroupBy(x => x)
				.OrderByDescending(x => x.Count())
				.Select(x => x.Key)
				.Take(5)
				.OrderBy(x => x)
				.GroupListItems(2)
				.ToList()
				.ForEach(x => values[i == 0 ? "COM" : "COM" + ++i] = x);
			return values;
		}

		private Dictionary<string, string> GetModificationsList(Transaction tr)
		{
			var values = new Dictionary<string, string>();
			Enumerable.Range(1, 5).ToList().ForEach(x => values.Add("MD" + x, ""));
			var i = 0;
			var list = QueryEntities<BlockReference>(tr, null, _plotParameters.DrawingExtend)
				.Where(x => string.Equals(x.Name, "GIS_MODF")).ToArray();
			list.Select(x => new
				{
					date = ParseDate(x.GetAttribute("DATE", string.Empty)),
					user = x.GetAttribute("USER", string.Empty),
					desc = x.GetAttribute("DESC", string.Empty),
				})
				.Distinct()
				.OrderByDescending(x => x.date)
				.Take(5)
				.ToList()
				.ForEach(x => values["MD" + ++i] = string.Format("{0:dd/MM/yyyy}-{1}-{2}", x.date, x.user, x.desc));
			return values;
		}

		private Dictionary<string, string> GetPlanchetteIdsList()
		{
			var i = 0;
			return GetSurroundedPlanchetteIds(_plotParameters.MapCoordinate, _plotParameters.PlanchetteLetter)
				.ToDictionary(x => "PL" + ++i);
		}

		private IEnumerable<string> GetSurroundedPlanchetteIds(Point2d planchettePosition, string planchetteLetter)
		{
			var letters = "ABCDEFGH";
			var l1 = letters.IndexOf(planchetteLetter, StringComparison.InvariantCultureIgnoreCase);
			var dx = new int[3];
			if("ABCD".Contains(planchetteLetter)) 
				dx[0] = -1;
			else 
				dx[2] = 1;
			var dy = new int[3];
			if ("AE".Contains(planchetteLetter)) dy[2] = -1;
			if ("DH".Contains(planchetteLetter)) dy[0] = 1;
			//var lettersOffset = new int[3, 3] { { 5, 1, 5 }, { 4, 0, 4 }, { 3, 7, 3 } };
			var lettersOffset = new int[3, 3] { { 5, 4, 3 }, { 1, 0, 7 }, { 5, 4, 3 } };
			for (var y = 2; y >= 0; y--)
			{
				for (var x = 0; x < 3; x++)
				{
					var l2 = letters[(l1 + lettersOffset[x, y] + dy[y] * 4) % 8];
					var px = Math.Truncate(planchettePosition.X / 1000) + dx[x];
					var py = Math.Truncate(planchettePosition.Y / 1000) + dy[y];
					yield return string.Format("{0}{1}{2}", px, py, l2);
				}
			}
		}

		private DateTime? ParseDate(string text)
		{
			var culture = new CultureInfo("fr-FR");
			var formats = new [] { "dd/MM/yyyy", "dd/MM/yy" };
			foreach (var format in formats)
			{
				DateTime dateResult;
				var result = DateTime.TryParseExact(text, format, culture, DateTimeStyles.None, out dateResult);
				if (result)
				{
					return dateResult;
				}
			}
			return null;
		}

		private void CreateAndConfigureLayout()
		{
			using (var tr = _document.Database.TransactionManager.StartTransaction())
			{
				using (var layout = CreatePlotLayout(tr))
				{
					SetPlotSettings(layout);
					using (var viewport = CreateViewport(layout, tr))
					{
						SetViewportSettings(viewport);
					}
					AddDrawingBorders(tr, layout);
					AddStamp(tr, layout);
				}
				tr.Commit();
			}
		}

		private void SetViewportSettings(Viewport viewport)
		{
			viewport.Width = _plotParameters.ViewPortSize.Width;
			viewport.Height = _plotParameters.ViewPortSize.Height;
			viewport.CenterPoint = _plotParameters.ViewPortCenterPoint;

			viewport.ViewDirection = new Vector3d(0, 0, 1);
			viewport.ViewCenter = _plotParameters.DrawingCenter;
			viewport.CustomScale = _plotParameters.Scale;
			viewport.Locked = true;
			viewport.On = true;

			Helper.Log("DrawingCenter: {0}-{1}", _plotParameters.DrawingCenter.X, _plotParameters.DrawingCenter.Y);
		}

		private Layout GetPlotLayout(Transaction tr)
		{
			var id = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
			//var id = LayoutManager.Current.GetLayoutId(_plotParameters.PlotLayoutName);
			var layout = (Layout) tr.GetObject(id, OpenMode.ForWrite);
			return layout;
		}

		private Layout CreatePlotLayout(Transaction tr)
		{
			var id = CreateAndMakeLayoutCurrent(_plotParameters.PlotLayoutName);
			var layout = (Layout) tr.GetObject(id, OpenMode.ForWrite);
			return layout;
		}

		private ObjectId CreateAndMakeLayoutCurrent(string name)
		{
			var id = LayoutManager.Current.GetLayoutId(name);
			if (!id.IsValid)
			{
				id = LayoutManager.Current.CreateLayout(name);
				LayoutManager.Current.CurrentLayout = name;
			}
			return id;
		}

		//private Viewport GetOrCreateViewport(Layout layout, Transaction tr, int vpNum)
		//{
		//    Viewport vp = null;
		//    //var vp = GetViewport(layout, tr, vpNum);
		//    //if (vp == null)
		//    //{
		//        vp = CreateViewport(layout, tr);
		//    //}
		//    return vp;
		//}

		//private Viewport GetViewport(Layout layout, Transaction tr, int vpNum)
		//{
		//    var vpIds = layout.GetViewports();
		//    foreach (ObjectId vpId in vpIds)
		//    {
		//        var vp = tr.GetObject(vpId, OpenMode.ForWrite) as Viewport;
		//        if (vp != null && vp.Number == vpNum)
		//        {
		//            return vp;
		//        }
		//    }
		//    return null;
		//}

		private Viewport CreateViewport(Layout layout, Transaction tr)
		{
			var btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);
			var vp = new Viewport();
			btr.AppendEntity(vp);
			tr.AddNewlyCreatedDBObject(vp, true);
			vp.On = true;
			vp.GridOn = true;
			return vp;
		}

		private void SetPlotSettings(Layout layout)
		{
			using (var ps = new PlotSettings(layout.ModelType))
			{
				ps.CopyFrom(layout);
				var psv = PlotSettingsValidator.Current;

				var devs = psv.GetPlotDeviceList();
				if (!devs.Contains(_plotParameters.Pc3Name))
				{
					throw new ArgumentException("PC3 not found " + _plotParameters.Pc3Name);
				}

				psv.SetPlotConfigurationName(ps, _plotParameters.Pc3Name, null);
				psv.RefreshLists(ps);

				_plotParameters.PageFormat = GetPageFormat(psv, ps, _plotParameters.PlotterName);

				psv.SetCanonicalMediaName(ps, _plotParameters.PageFormat.CanonicalMediaName);
				psv.RefreshLists(ps);

				var ssl = psv.GetPlotStyleSheetList();
				if (ssl.Contains(_plotParameters.PageFormat.CtbName))
				{
					psv.SetCurrentStyleSheet(ps, _plotParameters.PageFormat.CtbName);
				}
				else if (ssl.Contains(PlotConfiguration.Config.DefaultStyleSheet))
				{
					psv.SetCurrentStyleSheet(ps, PlotConfiguration.Config.DefaultStyleSheet);
				}
				else
				{
					throw new ArgumentException("No styleSheet found for " + _plotParameters.PageFormat.Pc3Name);
				}

				layout.CopyFrom(ps);
			}
		}

		private Size GetPageSize()
		{
			return string.IsNullOrEmpty(_plotParameters.PlanchetteId) ?
				new Size(_document.Database.Extmax.X - _document.Database.Extmin.X,
				_document.Database.Extmax.Y - _document.Database.Extmin.Y) : _plotParameters.PageSize;
		}

		private PageFormat GetPageFormat(PlotSettingsValidator psv, PlotSettings ps, string plotterName)
		{
			var pageSize = GetPageSize();
			Helper.Log("Drawing size: {0},{1}", pageSize.Width, pageSize.Height);
			IEnumerable<PageFormat> pageFormatList = null;
			if (_plotParameters.IsPlanchette)
			{
				pageFormatList = PlotConfiguration.Config.GetDefaultPageFormat(plotterName);
			}
			if (pageFormatList == null || !pageFormatList.Any())
			{
				pageFormatList = GetPageFormatList(psv, ps).ToArray();
			}
			pageFormatList = pageFormatList
				.OrderBy(x => x.PlotPaperSize.Width * x.PlotPaperSize.Height);
			var pageFormat = pageFormatList
				.FirstOrDefault(x => x.PlotPaperSize.Width >= pageSize.Width && x.PlotPaperSize.Height >= pageSize.Height);
			if (pageFormat == null && pageFormatList.Any())
			{
				pageFormat = pageFormatList.Last();
				pageFormat.ShrinkDrawing = true;
			}
			if (pageFormat == null)
			{
				throw new ArgumentException("No paper format found for " + plotterName);
			}
			Helper.Log("Selected paper format: {0}", pageFormat.CanonicalMediaName);
			return pageFormat;
		}

		//private IEnumerable<PageFormat> GetPageFormatList(string plotterName)
		//{
		//    var da = new DataAccessService(PlotConfiguration.Config.ConnectionString);
		//    var query = string.Format( 
		//          "SELECT DISTINCT a.DESCRIPTION, a.CONFIGURATIONFILE, a.COLORTABLE, b.ALTERNATESTYLENAME, "
		//        + "      c.ISDEFAULT, d.NAME, d.TYPE, d.WIDTH, d.HEIGHT "
		//        + "FROM cr_energis.PLOTTER a "
		//        + "INNER JOIN cr_energis.PLOTTERPROPERTIES b ON a.PLOTTERPROPERTIESID = b.PLOTTERPROPERTIESID "
		//        + "INNER JOIN cr_energis.PLOTPROPPAPERFORMAT c ON a.PLOTTERPROPERTIESID = c.PLOTTERPROPERTIESID "
		//        + "INNER JOIN cr_energis.PAPERFORMAT d ON c.PAPERFORMATID = d.PAPERFORMATID "
		//        + "WHERE a.DESCRIPTION = '{0}' ",
		//        plotterName);
		//    var list = da.IterateOverReader(query, x => new PageFormat()
		//        {
		//            PlotterName = x.GetString(0),
		//            Pc3Name = x.GetString(1),
		//            CtbName = x.GetString(2),
		//            AlternateStyleName = x.GetString(3),
		//            IsDefaultPaperSize = x.GetBoolean(4),
		//            CanonicalMediaName = x.GetString(5),
		//            FeederType = x.GetString(6),
		//            PlotPaperSize = new Size(x.GetInt32(7), x.GetInt32(8))
		//        });
		//    return list;
		//}

		private IEnumerable<PageFormat> GetPageFormatList(PlotSettingsValidator psv, PlotSettings ps)
		{
			var list = psv.GetCanonicalMediaNameList(ps);
			for (var i = 0; i < list.Count; i++)
			{
				psv.SetCanonicalMediaName(ps, list[i]);
				psv.RefreshLists(ps);
				var sizex = ps.PlotPaperSize.X
					- ps.PlotPaperMargins.MinPoint.X
					- ps.PlotPaperMargins.MaxPoint.X;
				var sizey = ps.PlotPaperSize.Y
					- ps.PlotPaperMargins.MinPoint.Y
					- ps.PlotPaperMargins.MaxPoint.Y;
				yield return new PageFormat()
				{
					Pc3Name = "",
					CanonicalMediaName = ps.CanonicalMediaName,
					PlotPaperSize = ps.PlotRotation == PlotRotation.Degrees090 ? new Size(sizey, sizex) : new Size(sizex, sizey)
				};
				if (_plotParameters.Debug)
				{
					Helper.Log("Page size: {0,-40} {1} {2} {3}", ps.CanonicalMediaName, ps.PlotPaperSize, ps.PlotPaperMargins.MinPoint, ps.PlotPaperMargins.MaxPoint);
				}
			}
		}

		private void SetPageView()
		{
			using (var view = _document.Editor.GetCurrentView())
			{
				var pageSize = _plotParameters.PageFormat.PlotPaperSize;
				view.Width = pageSize.Width;
				view.Height = pageSize.Height;
				view.CenterPoint = new Point2d(pageSize.Width / 2, pageSize.Height / 2);
				_document.Editor.SetCurrentView(view);
			}
		}

		//private string GetCanonicalMediaName(PlotSettingsValidator psv, PlotSettings ps, string pageSize)
		//{
		//    var list = psv.GetCanonicalMediaNameList(ps);
		//    for (var j = 0; j < list.Count; j++)
		//    {
		//        var name = psv.GetLocaleMediaName(ps, j);
		//        if (string.Equals(name, pageSize, StringComparison.InvariantCultureIgnoreCase))
		//        {
		//            return list[j];
		//        }
		//    }
		//    return null;
		//}

		//private int ParseResolutionParameter()
		//{
		//    var res = GetCommandLineParameterValue("r");
		//    int resolution;
		//    if (!int.TryParse(res, out resolution))
		//    {
		//        throw new ArgumentException("Resolution (r) not valid " + res);
		//    }
		//    return resolution;
		//}

		//private Point2d GetPointParameter(string prompt)
		//{
		//    var pr = _document.Editor.GetPoint("\n" + prompt + ":");
		//    if (pr.Status != PromptStatus.OK)
		//    {
		//        throw new ArgumentException("Invalid input for '" + prompt + "'");
		//    }
		//    return new Point2d(pr.Value.X, pr.Value.Y);
		//}

		//private string GetPlanchetteId()
		//{
		//    return PromptStringParameter("Get planchette position");
		//}

		//private string GetCommandLineParameterValue(string name)
		//{
		//    return null;
		//}

		//private string PromptStringParameter(string prompt)
		//{
		//    var pr = _document.Editor.GetString("\n" + prompt + ":");
		//    if (pr.Status != PromptStatus.OK)
		//    {            var args = Environment.GetCommandLineArgs();
		//        throw new ArgumentException("Invalid input for '" + prompt + "'");
		//    }
		//    return pr.StringResult.ToUpper();
		//}

		private void DeleteImportedFiles()
		{
			try
			{
				Helper.DeleteTempFolder(_tempFolder);
			}
			catch (System.Exception ex)
			{
				Helper.Log(ex.ToString());
			}
		}

		private void SaveDwg(string filePath)
		{
			var document = Application.DocumentManager.MdiActiveDocument;
			document.Database.SaveAs(filePath, true, DwgVersion.Current, document.Database.SecurityParameters);
		}

		private void PlotExtents()
		{
			using (var tr = _document.Database.TransactionManager.StartTransaction())
			{
				var layout = GetPlotLayout(tr);

				var ps = new PlotSettings(layout.ModelType);
				ps.CopyFrom(layout);
				ps.PlotPlotStyles = true;

				var psv = PlotSettingsValidator.Current;
				psv.SetDefaultPlotConfig(ps);
				psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
				psv.SetUseStandardScale(ps, true);
				//psv.SetPlotCentered(ps, true);
				
				psv.SetPlotOrigin(ps, PlotConfiguration.Config.PlotOrigin);
				if (_plotParameters.PageFormat.ShrinkDrawing)
				{
					psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
				}
				else
				{
					psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);
				}
				//psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
				//var extent2d = ps.PlotPaperMargins;
				//if (extent2d.MaxPoint.Y > extent2d.MaxPoint.X)
				//{
				//    psv.SetPlotRotation(ps, PlotRotation.Degrees000);
				//}
				//else
				//{
				//    psv.SetPlotRotation(ps, PlotRotation.Degrees090);
				//}
				psv.SetPlotConfigurationName(ps, _plotParameters.Pc3Name, _plotParameters.PageFormat.CanonicalMediaName);
				psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);

				var pi = new PlotInfo();
				pi.Layout = layout.ObjectId;
				pi.OverrideSettings = ps;

				var piv = new PlotInfoValidator();
				piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
				piv.Validate(pi);

				using (var pe = PlotFactory.CreatePublishEngine())
				using (var ppi = new PlotPageInfo())
				{
					pe.BeginPlot(null, null);
					pe.BeginDocument(pi, _document.Name, null, 1, _plotParameters.PlotToFile, _plotParameters.OutputFilePath);
					pe.BeginPage(ppi, pi, true, null);
					pe.BeginGenerateGraphics(null);
					pe.EndGenerateGraphics(null);
					pe.EndPage(null);
					pe.EndDocument(null);
					pe.EndPlot(null);
				}

				tr.Commit();
			}
		}

		/*
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE,POSTPROCESSOR) VALUES (PLOTTER_ID_SEQ.NEXTVAL,'PlotWave 750 - EDLX AYE - Bureau de dessin',      'PlotWave 750 - EDLX AYE - Bureau de dessin.pc3', 'PlotWave 750 - EDLX AYE - Bureau de dessin.ctb',        50,1,0,'PlotClient',null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE,POSTPROCESSOR) VALUES (PLOTTER_ID_SEQ.NEXTVAL,'PlotWave 900 - EDH SYA - Bureau de dessin',       'PlotWave 900 - EDH SYA - Bureau de dessin.pc3',  'PlotWave 900 - EDH SYA - Bureau de dessin.ctb',         51,1,0,'PlotClient',null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE,POSTPROCESSOR) VALUES (PLOTTER_ID_SEQ.NEXTVAL,'PlotWave 900 - EDH SYA - Bureau de dessin (PS)',  'Oce TDS860 - Plot Serveur (PTF).pc3',            'PlotWave 900 - EDH SYA - Bureau de dessin.ctb',         32,1,0,'PlotServer','PLOTWAVE-EDH-SYA');
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE,POSTPROCESSOR) VALUES (PLOTTER_ID_SEQ.NEXTVAL,'Oce TDS860 - Plot Serveur (PTF)',                 'Oce TDS860 - Plot Serveur (PTF).pc3',            'Oce TDS860 - Plot Serveur (PTF).ctb',                   32,1,0,'PlotServer',null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE,POSTPROCESSOR) VALUES (PLOTTER_ID_SEQ.NEXTVAL,'PlotWave 750 - EDLX AYE - Bureau de dessin (PS)', 'Oce TDS860 - Plot Serveur (PTF).pc3',            'PlotWave 750 - EDLX AYE - TDS860 Bureau de dessin.ctb', 32,1,0,'PlotServer','PLOTWAVE-EDLX-AYE');
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE)               VALUES (PLOTTER_ID_SEQ.NEXTVAL,'PDF',                                             'PDF.pc3',                                        'PDF.ctb',                                               37,1,1,null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE)               VALUES (PLOTTER_ID_SEQ.NEXTVAL,'DWF',                                             'DWF6.pc3',                                       'DWF6.ctb',                                              38,0,1,null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE)               VALUES (PLOTTER_ID_SEQ.NEXTVAL,'DWF6 ePlot',                                      'DWF6 ePlot.pc3',                                 'DWF6 ePlot.ctb',                                        39,0,1,null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE)               VALUES (PLOTTER_ID_SEQ.NEXTVAL,'DWF6CapGIS.pc3',                                  'DWF6CapGIS.pc3',                                 'DWF6CapGIS.ctb',                                        40,0,1,null);
		INSERT INTO PLOTTER(PLOTTERID,DESCRIPTION,CONFIGURATIONFILE,COLORTABLE,PLOTTERPROPERTIESID,ISEDITABLE,ISDEFAULTINSTALLED,REQUIREDPRIVILEGE)               VALUES (PLOTTER_ID_SEQ.NEXTVAL,'Impetrant',                                       'Impetrant.pc3',                                  'Impetrant.ctb',                                         44,1,1,null);
		 
		INSERT INTO PLOTTERPROPERTIES(PLOTTERPROPERTIESID,MODEL,PLOTTERTYPE,ALLOWMULTIPLECOPIES,ISCOLOR,ISMIRRORABLE,ALTERNATESTYLENAME,MARGIN,OPTIMIZE) VALUES (32,'Oce TDS860',    'OCE_PLOTTER', 0,0,1,'PLOTOCEGRAY',4,0);
		INSERT INTO PLOTTERPROPERTIES(PLOTTERPROPERTIESID,MODEL,PLOTTERTYPE,ALLOWMULTIPLECOPIES,ISCOLOR,ISMIRRORABLE,ALTERNATESTYLENAME,MARGIN,OPTIMIZE) VALUES (50,'Plot Wave 750', 'PLOTTER',     0,0,0,'PLOTOCEGRAY',0,0);
		INSERT INTO PLOTTERPROPERTIES(PLOTTERPROPERTIESID,MODEL,PLOTTERTYPE,ALLOWMULTIPLECOPIES,ISCOLOR,ISMIRRORABLE,ALTERNATESTYLENAME,MARGIN,OPTIMIZE) VALUES (51,'Oce TDS860',    'PLOTTER',     0,0,0,'PLOTOCEGRAY',0,0);

		 * 

		SELECT a.DESCRIPTION, a.CONFIGURATIONFILE, a.COLORTABLE, b.ALTERNATESTYLENAME,
			   c.ISDEFAULT, d.NAME, d.TYPE, d.WIDTH, d.HEIGHT
		FROM cr_energis.PLOTTER a
		INNER JOIN cr_energis.PLOTTERPROPERTIES b ON a.PLOTTERPROPERTIESID = b.PLOTTERPROPERTIESID
		INNER JOIN cr_energis.PLOTPROPPAPERFORMAT c ON a.PLOTTERPROPERTIESID = c.PLOTTERPROPERTIESID
		INNER JOIN cr_energis.PAPERFORMAT d ON c.PAPERFORMATID = d.PAPERFORMATID


		select c.*, b.*, a.* 
		FROM PAPERFORMAT a
		INNER JOIN PLOTPROPPAPERFORMAT b ON a.PAPERFORMATID = b.PAPERFORMATID
		INNER JOIN PLOTTER c ON c.PLOTTERPROPERTIESID = b.PLOTTERPROPERTIESID"

				 * 
				 * 
				 'PlotWave 900 - EDH SYA - Bureau de dessin.pc3' 
				 'Oce TDS860 - Plot Serveur (PTF).pc3'
		null,  'PLOTOCEGRAY',   'ViewportVisibility=0;LayerName=BR##,BB##,BF##,BH##,BG##,BL##,BP##,BT##,BW##,W####;Operator=<NOT;EntityType=TEXT;Operator=NOT>',                               'Color=252');
		null,  'PLOTOCEGRAY',   'ViewportVisibility=0;Operator=<NOT;LayerName=BR##,BB##,BF##,BH##,BG##,BL##,BP##,BT##,BW##,W####;Operator=NOT>;Operator=<NOT;EntityType=IMAGE;Operator=NOT>;', 'Color=7');
		'MAP', 'PLOTOCEGRAY',   'EntityType=TEXT;TextHeight<{jobScaleFactor * jobMinTextHeight}',                                                                                              'TextHeight={jobScaleFactor * jobMinTextHeight}');

		'MAP', 'PLOTDEFAULT',   'EntityType=TEXT;TextHeight<{jobScaleFactor * jobMinTextHeight}',                                                                                              'TextHeight={jobScaleFactor * jobMinTextHeight}');

		null,  'IMPETRANTGRAY', 'ViewportVisibility=0;LayerName=BR##,BB##,BF##,BH##,BG##,BL##,BP##,BT##,BW##,W####;Operator=<NOT;EntityType=TEXT;Operator=NOT>',                               'Color=252');
		null,  'IMPETRANTGRAY', 'ViewportVisibility=0;LayerName=BR##,BB##,BF##,BH##,BG##,BL##,BP##,BT##,BW##,W####;EntityType=TEXT;',                                                          'Color=7');

				Hide layers:
				BR05,niv_voirie,w0001,W0060,W0070

				 EntityType=TEXT;
				TextHeight<{jobScaleFactor * jobMinTextHeight} 
				=>TextHeight={jobScaleFactor * jobMinTextHeight}
				 jobMinTextHeight = MinTextHeight =  PlotDefaultHtMin(in db)       = 1.5
				 jobScaleFactor   = xxx           = PlotDefaultLayoutScale (in db) = 1000
				*/

		private void ApplyOceGrayStyle(Transaction tr)
		{
			var regex = new Regex(PlotConfiguration.Config.TopoLayersRegexFilter, RegexOptions.IgnoreCase);
			var list = QueryEntities(tr, null, _plotParameters.DrawingExtend);
			foreach (var entity in list)
			{
				if (regex.IsMatch(entity.Layer))
				{
					if (!(entity is DBText || entity is MText))
					{
						entity.UpgradeOpen();
						entity.ColorIndex = 252;
						entity.DowngradeOpen();
					}
				}
				else
				{
					if (!(entity is Image))
					{
						entity.UpgradeOpen();
						entity.ColorIndex = 7;
						entity.DowngradeOpen();
					}
				}
			}
		}

		private void ApplyImpetrantStyle(Transaction tr)
		{
			var list = QueryEntities(tr, PlotConfiguration.Config.TopoLayersRegexFilter,
				_plotParameters.DrawingExtend.Inflate(PlotConfiguration.Config.InternalBorderWidth));
			foreach (var entity in list)
			{
				entity.UpgradeOpen();
				if (entity is DBText)
				{
					entity.ColorIndex = 7;
				}
				else if (entity is MText)
				{
					//var mtext = (MText)entity;
					//if (mtext.TextHeight < 1.5) mtext.TextHeight = 1.5;
					entity.ColorIndex = 7;
				}
				else
				{
					entity.ColorIndex = 252;
				}
				entity.DowngradeOpen();
			}
		}

		private void DeleteNotNeededLayers(Database db)
		{
			var regex = new Regex(PlotConfiguration.Config.LayersToDeleteRegexFilter, RegexOptions.IgnoreCase);
			using (var tr = db.TransactionManager.StartTransaction())
			{
				var layerIds = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
				foreach (var id in layerIds)
				{
					var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite);
					if (regex.IsMatch(layer.Name))
					{
						layer.IsLocked = false;

						var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
						foreach (var btrId in blockTable)
						{
							var block = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
							foreach (var entId in block)
							{
								var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
								if (string.Equals(ent.Layer, layer.Name, StringComparison.InvariantCultureIgnoreCase))
								{
									ent.UpgradeOpen();
									ent.Erase();
								}
							}
						}

						layer.Erase(true);
					}
				}
				tr.Commit();
			}
		}
	}

	internal class PlotParameters
	{
		public PlotParameters(string[] args)
		{
			for (var i = 1; i < args.Length; i++)
			{
				switch (args[i].ToLower())
				{
					case @"/r":
						Resolution = int.Parse(args[++i]);
						break;
					case @"/c":
						Category = args[++i];
						break;
					case @"/e":
						Energies = args[++i].Split(',');
						break;
					case @"/p":
						PlotterName = args[++i];
						break;
					case @"/id":
						SetPlanchetteId(args[++i]);
						break;
					case @"/f":
						SetOutputFilePath(args[++i]);
						break;
					case @"/imp":
						SetImpetrant();
						break;
					case @"/z":
						Zone = (Zone) Enum.Parse(typeof(Zone), args[++i]);
						break;
					case @"/d":
						Debug = true;
						break;
					case @"/st":
						StampId = args[++i];
						break;
					case @"/t":
						PlanTotal = int.Parse(args[++i]);
						break;
					case @"/n":
						PlanId = int.Parse(args[++i]);
						break;
					case @"/u":
						UserId = args[++i];
						break;
					case @"/i":
					case @"/l":
					case @"/s":
						++i;
						break;
					case @"/isolate":
						break;
					default:
						throw new ArgumentException("Unknown argument " + args[i]);
				}
			}
			Validate();
		}

		// PlotCardexEnerGISCommand => no stamp
		//public string JobType = "Structured";
		//public string s_plot_ticket = "952926";
		//public string s_plot_request = "129415";
		//public string c_type_plan = "T";
		//public string l_div	= "La Louvière";
		//public string l_path_plan = "";


		// /st "1629628-29519681" /t = 7 /n 1 /u ADN534;

		// l_id_stamp
		public string StampId { get; private set; }
		// n_tot_plan
		public int PlanTotal { get; private set; }
		// n_ord_plan
		public int PlanId { get; private set; }
		// userid
		public string UserId { get; private set; }
		// l_id_planchette
		public string PlanchetteId { get; private set; }
		// l_path_result_pdf
		public string OutputFilePath { get; private set; }
		// n_scale
		public int Resolution { get; private set; }
		// c_type_map
		public string Category { get; private set; }
		// list_energy
		public string[] Energies { get; private set; }

		public bool Impetrant { get; private set; }

		public string PlotterName { get; private set; }

		//public string CanonicalMediaName { get; set; }

		public string PlanchetteLetter { get; private set; }

		public Point2d MapCoordinate { get; private set; }

		public Boolean Debug { get; private set; }

		public Zone Zone { get; set; }

		//public string StyleSheet
		//{
		//    get { return Path.GetFileNameWithoutExtension(PlotterName) + ".ctb"; }
		//}

		public Size ViewPortSize
		{
			get { return new Size(PlotConfiguration.Config.DrawingSize.Width * Scale 
				+ 2 * PlotConfiguration.Config.InternalBorderWidth, 
				PlotConfiguration.Config.DrawingSize.Height * Scale 
				+ 2 * PlotConfiguration.Config.InternalBorderWidth); }
		}

		public Point3d ViewPortCenterPoint
		{
			get
			{
				var s = ViewPortSize;
				return new Point3d(s.Width / 2 + PlotConfiguration.Config.ExternalBorderWidth,
					s.Height / 2 + PlotConfiguration.Config.ExternalBorderWidth, 0);
			}
		}


		public double Scale
		{
			get { return 1000.0 / Resolution; }
		}

		public Extents2d DrawingExtend
		{
			get
			{
				return new Extents2d(MapCoordinate.X, MapCoordinate.Y,
					MapCoordinate.X + PlotConfiguration.Config.DrawingSize.Width,
					MapCoordinate.Y + PlotConfiguration.Config.DrawingSize.Height);
			}
		}

		public Point2d DrawingCenter
		{
			get
			{
				var e = DrawingExtend;
				return new Point2d((e.MinPoint.X + e.MaxPoint.X) / 2,
					(e.MinPoint.Y + e.MaxPoint.Y) / 2);
			}
		}

		public Point3d PlotCartridgePosition 
		{
			get
			{
				var position = PlotConfiguration.Config.DrawingSize.Width * Scale
					+ 2 * PlotConfiguration.Config.InternalBorderWidth
					+ 2 * PlotConfiguration.Config.ExternalBorderWidth
					+ PlotConfiguration.Config.CartridgeExternalBorderWidth;
				return new Point3d(position, 0, 0);
			}
		}

		public Point3d StampPosition 
		{
			get
			{
				var x = PageSize.Width - PlotConfiguration.Config.CartridgeExternalBorderWidth + 2;
				return new Point3d(x, 0, 0);
			}
		}

		public Size PageSize 
		{
			get
			{
				var height = PlotConfiguration.Config.DrawingSize.Height * Scale 
					+ 2 * PlotConfiguration.Config.InternalBorderWidth
					+ 2 * PlotConfiguration.Config.ExternalBorderWidth;
				var width = PlotConfiguration.Config.DrawingSize.Width * Scale 
					+ 2 * PlotConfiguration.Config.InternalBorderWidth
					+ 2 * PlotConfiguration.Config.ExternalBorderWidth
					+ PlotConfiguration.Config.PlotCartridgeWidth 
					+ 2 * PlotConfiguration.Config.CartridgeExternalBorderWidth;
				return new Size(width, height);
			}
		}

		public string CartridgeTemplate
		{
			get { return Zone == Zone.Est ? PlotConfiguration.Config.EstCartridgeTemplateFilePath : PlotConfiguration.Config.WestCartridgeTemplateFilePath; }
		}

		public string FileServerName
		{
			get { return Zone == Zone.Est ? PlotConfiguration.Config.EstFileServerName : PlotConfiguration.Config.WestFileServerName; }
		}

		public string Stamp
		{
			get
			{
				return string.Format("ORES {0:dd.MM.yy}-{1}-{2}-{3}/{4}",
					DateTime.Now, PlotConfiguration.Config.ServerName, StampId, PlanId, PlanTotal);
			}
		}

		public string PlotLayoutName
		{
			get
			{
				return string.IsNullOrEmpty(PlanchetteId) ? "Model" : PlotConfiguration.Config.PlotLayoutName;
			}
		}

		public PageFormat PageFormat { get; set; }
		public IEnumerable<string> EnergyDescription { get; set; }
		public string Pc3Name 
		{
			get
			{
				var pageFormats = PlotConfiguration.Config.GetDefaultPageFormat(PlotterName);
				if (pageFormats.Any())
				{
					return pageFormats.First().Pc3Name;
				}
				return PlotterName + ".pc3";
			}
		}

		public bool IsPlanchette 
		{
			get
			{
				return string.IsNullOrEmpty(PlanchetteId);
			}
		}

		public bool PlotToFile 
		{
			get
			{
				return !string.IsNullOrEmpty(OutputFilePath);
			}
		}

		private void Validate()
		{
			if (!string.IsNullOrEmpty(PlanchetteId) && string.IsNullOrEmpty(Category))
			{
				throw new ArgumentException("Category (/c) missing");
			}
			if (string.IsNullOrEmpty(PlotterName) && string.IsNullOrEmpty(OutputFilePath))
			{
				throw new ArgumentException("Either the output file (/o) or the plotter pc3 (/p) required");
			}
			
			//if (Impetrant && !string.IsNullOrEmpty(OutputFilePath) && Path.GetExtension(OutputFilePath) != ".pdf")
			//{
			//    throw new ArgumentException("Impetrant is for pdf file only");
			//}
			//if (Impetrant && !string.Equals(Pc3Name, "Impetrant.pc3"))
			//{
			//    throw new ArgumentException("Impetrant is for pdf file only");
			//}
		}

		private void SetPlanchetteId(string planchetteId)
		{
			PlanchetteId = planchetteId;
			string letter;
			MapCoordinate = ParsePlanchetteId(planchetteId, out letter);
			PlanchetteLetter = letter;
		}

		private void SetOutputFilePath(string outputFilePath)
		{
			OutputFilePath = outputFilePath;
			if (string.IsNullOrEmpty(PlotterName))
			{
				SetDiviceAndStyleSheet();
			}
		}

		private void SetImpetrant()
		{
			Impetrant = true;
			if (string.IsNullOrEmpty(PlotterName) || PlotterName == "PDF")
			{
				SetDiviceAndStyleSheet();
			}
		}

		private Point2d ParsePlanchetteId(string planchetteId, out string planchetteLetter)
		{
			var regex = new Regex(@"^(?<x>[0-9]{3})(?<y>[0-9]{3})(?<letter>[A-H])$", RegexOptions.IgnoreCase);
			var match = regex.Match(planchetteId);
			if (!match.Success)
			{
				throw new ArgumentException("Planchette Id not valid " + planchetteId);
			}
			var x = int.Parse(match.Groups["x"].Value) * 1000;
			var y = int.Parse(match.Groups["y"].Value) * 1000;
			planchetteLetter = match.Groups["letter"].Value;
			var dx = 0;
			var dy = 0;
			if ("ABCD".Contains(planchetteLetter))
			{
				dx = 0;
				dy = "ABCD".IndexOf(planchetteLetter);
			}
			else
			{
				dx = 1;
				dy = "EFGH".IndexOf(planchetteLetter);
			}
			y = Convert.ToInt32(y + dy * PlotConfiguration.Config.DrawingSize.Height);
			x = Convert.ToInt32(x + dx * PlotConfiguration.Config.DrawingSize.Width);
			return new Point2d(x, y);
		}

		private void SetDiviceAndStyleSheet()
		{
			if (Impetrant)
			{
				PlotterName = "Impetrant";
			}
			else
			{
				switch (Path.GetExtension(OutputFilePath))
				{
					case ".pdf":
						PlotterName = "PDF";
						break;
					case ".dwf":
					case ".dwg":
						PlotterName = "DWF6";
						break;
					default:
						throw new ArgumentException(string.Format("Output file format {0} not suported", OutputFilePath));
				}
			}
		}
	}

	public enum Zone
	{
		Est,
		West
	}
}



		//using (Circle cir = new Circle(pr.Value, Vector3d.ZAxis, pr1.Value))
		//{
		//    using (Transaction tr = db.TransactionManager.StartTransaction())
		//    {
		//        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
		//        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
		//        cir.TransformBy(ed.CurrentUserCoordinateSystem);
		//        btr.AppendEntity(cir);
		//        tr.AddNewlyCreatedDBObject(cir, true);
		//        tr.Commit();
		//    }
		//}

		//using (Circle cir = new Circle(pr.Value, Vector3d.ZAxis, pr1.Value))
		//{
		//    using (Transaction tr = db.TransactionManager.StartTransaction())
		//    {
		//        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
		//        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite);
		//        cir.TransformBy(ed.CurrentUserCoordinateSystem);
		//        btr.AppendEntity(cir);
		//        tr.AddNewlyCreatedDBObject(cir, true);
		//        tr.Commit();
		//    }
		//}

		//using (Circle cir = new Circle(pr.Value, Vector3d.ZAxis, pr1.Value))
		//{
		//    using (Transaction tr = db.TransactionManager.StartTransaction())
		//    {
		//        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
		//        cir.TransformBy(ed.CurrentUserCoordinateSystem);
		//        btr.AppendEntity(cir);
		//        tr.AddNewlyCreatedDBObject(cir, true);
		//        tr.Commit();
		//    }
		//}
	
/*
===============================================================================================


DROP TABLE PTASK;
DROP TABLE PJOB;
DROP SEQUENCE PTASK_SEQ;
DROP SEQUENCE PJOB_SEQ;


CREATE TABLE PJOB (
	PJOBID	    NUMBER(9) not null,
"S_PLOT_TICKET"     NUMBER(12,0), 
"S_PLTICKET_STATUS" NUMBER(12,0), 
"O_DATE"            DATE, 
"N_TOT_PLAN"        NUMBER(5,0), 
"USERID"            VARCHAR2(6 BYTE), 
"C_ERROR"           VARCHAR2(50 BYTE), 
"DESC_ERROR"        VARCHAR2(500 BYTE), 
  CONSTRAINT IDX_PJOB_PK PRIMARY KEY (PJOBID)
) TABLESPACE "ENERGIS";

GRANT SELECT,INSERT,UPDATE,DELETE ON PJOB TO GENERGIS;
	
CREATE SEQUENCE PJOB_SEQ
	START WITH 1
	INCREMENT BY 1
	MINVALUE 1
	NOMAXVALUE;

GRANT SELECT ON PJOB_SEQ							TO GENERGIS;

------------------------------------------


CREATE TABLE PTASK (
	PTASKID	    NUMBER(9) not null,
	PJOBID	    NUMBER(9) not null,
"C_TYPE_PLAN"       CHAR(1 BYTE), 
"L_ID_STAMP"        VARCHAR2(25 BYTE), 
"L_ID_PLANCHETTE"   VARCHAR2(40 BYTE), 
"N_ORD_PLAN"        NUMBER(5,0), 
"C_TYPE_MAP"        CHAR(3 BYTE), 
"L_PATH_PLAN"       CLOB, 
"LIST_ENERGY"       VARCHAR2(50 BYTE), 
"L_PATH_RESULT_PDF" CLOB, 
"N_SCALE"           VARCHAR2(10 BYTE), 
"N_ESSAY"           NUMBER(1,0), 
"S_PLTICKET_STATUS" NUMBER(12,0), 
"C_SIDE"            CHAR(1 BYTE),
  CONSTRAINT IDX_PTASK_PK PRIMARY KEY (PTASKID),
  CONSTRAINT FK_PTASK_PJOB FOREIGN KEY (PJOBID) REFERENCES PJOB(PJOBID)
) TABLESPACE "ENERGIS";

GRANT SELECT,INSERT,UPDATE,DELETE ON PTASK TO GENERGIS;
	
CREATE SEQUENCE PTASK_SEQ
	START WITH 1
	INCREMENT BY 1
	MINVALUE 1
	NOMAXVALUE;

GRANT SELECT ON PTASK_SEQ							TO GENERGIS;

-------------------------------------------------

DROP SYNONYM GENERGIS.PJOB;
CREATE SYNONYM GENERGIS.PJOB			FOR CR_ENERGIS.PJOB;
DROP SYNONYM GENERGIS.PJOB_SEQ;
CREATE SYNONYM GENERGIS.PJOB_SEQ		FOR CR_ENERGIS.PJOB_SEQ;

DROP SYNONYM GENERGIS.PTASK;
CREATE SYNONYM GENERGIS.PTASK			FOR CR_ENERGIS.PTASK;
DROP SYNONYM GENERGIS.PTASK_SEQ;
CREATE SYNONYM GENERGIS.PTASK_SEQ		FOR CR_ENERGIS.PTASK_SEQ;
  

 */



