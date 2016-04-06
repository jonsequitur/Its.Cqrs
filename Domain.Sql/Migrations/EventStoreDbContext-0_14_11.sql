if not exists(select * from sys.indexes where name='IX_AggregateId_StreamName' and object_id = OBJECT_ID('eventstore.events'))
begin
	CREATE NONCLUSTERED INDEX [IX_AggregateId_StreamName] ON [EventStore].[Events] 
		([AggregateId], 
		 [StreamName]) 
	INCLUDE 
		([Actor], 
		 [Body], 
		 [ETag], 
		 [Id], 
		 [SequenceNumber], 
		 [Type], 
		 [UtcTime]) 
	WITH (ONLINE = OFF)
end