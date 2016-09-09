using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;

namespace BatchPlot.Extensions
{
    public static class BlockReferenceExtension
    {
        public static void UpdateAttributes(this BlockReference blockReference, Dictionary<string, string> values)
        {
            var tr = blockReference.Database.TransactionManager.TopTransaction;
            foreach (ObjectId attribute in blockReference.AttributeCollection)
            {
                if (!attribute.IsErased)
                {
                    using (var reference = (AttributeReference)tr.GetObject(attribute, OpenMode.ForRead))
                    {
                        if (reference != null && values.ContainsKey(reference.Tag))
                        {
                            reference.UpgradeOpen();
                            reference.TextString = values[reference.Tag];
                            reference.AdjustAlignment(blockReference.Database);
                        }
                    }
                }
            }
        }

        public static void CopyAttributeDefinition(this BlockTableRecord tableRecord, BlockReference reference, Dictionary<string, string> values)
        {
            var tr = tableRecord.Database.TransactionManager.TopTransaction;
            if (tableRecord.HasAttributeDefinitions)
            {
                foreach (var objectId in tableRecord)
                {
                    using (var dbObject = objectId.GetObject(OpenMode.ForRead))
                    {
                        var attrDef = dbObject as AttributeDefinition;
                        if (attrDef != null && !attrDef.Constant)
                        {
                            using (var attrRef = new AttributeReference())
                            {
                                attrRef.SetAttributeFromBlock(attrDef, reference.BlockTransform);
                                attrRef.Position = attrDef.Position.TransformBy(reference.BlockTransform);
                                if (values.ContainsKey(attrDef.Tag))
                                {
                                    attrRef.TextString = values[attrDef.Tag];
                                }
                                reference.AttributeCollection.AppendAttribute(attrRef);
                                tr.AddNewlyCreatedDBObject(attrRef, true);
                            }
                        }
                    }
                }
            }
        }

        public static T GetAttribute<T>(this BlockReference blockReference, string attribute, T defaultValue)
        {
            var tr = blockReference.Database.TransactionManager.TopTransaction;
            var returnValue = defaultValue;
            foreach (ObjectId current in blockReference.AttributeCollection)
            {
                if (!current.IsErased)
                {
                    using (var dbObject = tr.GetObject(current, OpenMode.ForRead))
                    {
                        var attributeDefinition = dbObject as AttributeReference;
                        if (attributeDefinition != null 
                            && attributeDefinition.Tag.Equals(attribute, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var stringValue = attributeDefinition.IsMTextAttribute
                                ? attributeDefinition.MTextAttribute.Text
                                : attributeDefinition.TextString;
                            var converter = TypeDescriptor.GetConverter(typeof(T));
                            returnValue = (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, stringValue);
                        }
                    }
                }
            }
            return returnValue;
        }
    }
}