# TgBotEnterprise
Pet project. Telegram bot "like enterprise way" 

This project is written for getting some experience in the “enterprise way” of developing.

The bot allows the user to get information about the state of “current version” from BugTracker (Redmine), CI system (Jenkins). 
Also add “webadmin” pages.

![alternative text](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/UseCase.puml)


I want to do a service for each communication system.
Therefore TgBot is needed the followed services:

![alternative text](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/Components.puml)


MainBot - Main service.  Contains all logic about bot-working.

Telegram service - Service for communication with telegram servers. Contains logic about boundies of telegram. Contains information about ralation of Telegram user and Bot User.

WebAdmin - web site. Contains logic of administrating TgBot.

Redmine service - service for communication to Redmine. Pulling changed tasks, generate bot-event about changed tasks, retrieval information for user requests.

Jenkins service - service for communication to Jenkins. Pulling changed jobs. Maybe, start new jobs, and so on.

Git service - Getting information from git.

Other nodes:

RabbitMQ - All communications between services are carried out through queues. I got RabbitMQ. Because it is a bit simpler than Kafka.

Postgre - Database for storing data of services. Each service has its own database.

As a logger server I choose Seq. It's a simple but very useful server. I put logs into Seq via Serilog.





[Underconstraction]

![alternative text](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/ProcessingTelegramMessage.puml)

