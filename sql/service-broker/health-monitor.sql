SELECT * FROM sys.symmetric_keys;
SELECT * FROM sys.certificates;
SELECT * FROM sys.service_broker_endpoints;

SELECT * FROM sys.tcp_endpoints AS i
INNER JOIN sys.service_broker_endpoints AS e
ON i.endpoint_id = e.endpoint_id;

SELECT * FROM sys.service_message_types;

SELECT * FROM sys.database_principals;

SELECT * FROM sys.routes;
SELECT * FROM sys.remote_service_bindings;
SELECT * FROM sys.transmission_queue;
SELECT * FROM sys.conversation_endpoints;

SELECT
--REPLACE(service_name, N'/service/', N'/queue/') AS [Queue],
CAST(SUBSTRING(message_body, 1, 128) AS varchar(max)) AS [UTF-8],
DATALENGTH(message_body) AS [Size]
FROM [dbo].[514B05BB-CD37-4D17-828C-AFFB5161380B/queue/test];

SELECT * FROM sys.services
SELECT * FROM sys.service_queues
SELECT * FROM sys.service_queue_usages
SELECT * FROM sys.conversation_endpoints