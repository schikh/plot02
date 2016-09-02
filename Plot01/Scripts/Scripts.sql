select c.*, b.*, a.* from PAPERFORMAT a
join PLOTPROPPAPERFORMAT b ON a.PAPERFORMATID = b.PAPERFORMATID
join PLOTTER c on c.PLOTTERPROPERTIESID = b.PLOTTERPROPERTIESID




-- monitoring des jobs d'impression des PS en fonction des flux
select * from CR_ENERGIS.PLOT_JOB_LOG pjl
left join plot_jobcontent_log pjcl on pjl.PLOTJOBLOGID = pjcl.PLOTJOBLOGID
--where pjl.QUEUENAME = 'NetGISQueue'
where pjl.QUEUENAME = 'CardexQueue'
--where pjl.QUEUENAME = 'ImpetrantQueue'
----and pjl.STATUS = 3
--and pjl.status = 5
--and pjl.STATUSDETAILS like '%Failed%'
--and pjl.STATUSDETAILS like '%Time%'
and pjl.statusdatetime >= to_date('27/06/2016 14:30:00','dd/mm/yyyy HH24:MI:SS')
--and pjl.servername = 'OUEST1'
--and pjcl.VALUE = 'EL564-4'
--and pjl.ticketid like '%Tournai%'
--and pjcl.LARGEVALUE like '%UPKCI098621_3681207021035%'
order by pjl.STATUSDATETIME desc;