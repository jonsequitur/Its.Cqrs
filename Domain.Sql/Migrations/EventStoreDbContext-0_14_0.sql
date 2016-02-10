if not exists(select * from sys.indexes where name='IX_ETag' and object_id = OBJECT_ID('eventstore.events'))
begin
	CREATE NONCLUSTERED INDEX [IX_ETag] ON [EventStore].[Events]
	(
		[ETag] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name='IX_StreamName' and object_id = OBJECT_ID('eventstore.events'))
begin
	CREATE NONCLUSTERED INDEX [IX_StreamName] ON [EventStore].[Events]
	(
		[StreamName] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name='IX_Type' and object_id = OBJECT_ID('eventstore.events'))
begin
	CREATE NONCLUSTERED INDEX [IX_Type] ON [EventStore].[Events]
	(
		[Type] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name='IX_Id_and_Type' and object_id = OBJECT_ID('eventstore.events'))
begin
	CREATE NONCLUSTERED INDEX [IX_Id_and_Type] ON [EventStore].[Events] 
	(
		[Id] ASC,
		[Type] ASC
	)
	INCLUDE ([StreamName])
end


