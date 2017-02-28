# rabbitmq_tester
Sender and Receiver via rabbitmq broker with SSL.

Create multiple receivers.

 ```
 cd src/Receiver
 dotnet run "amqps://user:passwd@127.0.0.1:5671/vhost" "/path/to/rabbitmq_ssl/client/cert.pem"
 ```

Send message using sender app. 

 ```
 cd src/Sender
 dotnet run "amqps://user:passwd@127.0.0.1:5671/vhost" "/path/to/rabbitmq_ssl/client/cert.pem"
 ```
