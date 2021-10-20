# TgBotEnterprise
Pet project. Telegram bot "like enterprise way" 

This project is written for getting some experience in the “enterprise way” of developing.


### UseCases

The bot allows the user to get information about the state of “current version” from BugTracker (Redmine), CI system (Jenkins). 
Also add “webadmin” pages.

![UseCase diagram by PlantUML](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/UseCase.puml)


## Services

I want to do a service for each communication system.
Therefore TgBot is needed the followed services:

![Components diagram by PlantUML](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/Components.puml)


**MainBot** - Main service.  Contains all logic about bot-working.

**Telegram service** - Service for communication with telegram servers. Contains logic about boundies of telegram. Contains information about ralation of Telegram user and Bot User.

**WebAdmin** - web site. Contains logic of administrating TgBot.

**Redmine service** - service for communication to Redmine. Pulling changed tasks, generate bot-event about changed tasks, retrieval information for user requests.

**Jenkins service** - service for communication to Jenkins. Pulling changed jobs. Maybe, start new jobs, and so on.

**Git service** - Getting information from git.

Other nodes:

**RabbitMQ** - All communications between services are carried out through queues. I got RabbitMQ. Because it is a bit simpler than Kafka.

**Postgre** - Database for storing data of services. Each service has its own database.

As a logger server I choose **Seq**. It's a simple but very useful server. I put logs into Seq via Serilog.

### Messages producers

Who are generated messages in system?






## Communications between services
_Asynchronous communications_ - standart messaging throught rabbit. Publish message to "CantralHub".

_Synchronous communications_ - emulation of synchronous execution. I need it for forming webpages and so on.
- Service 1
    - Create Task from TaskCompletionSource and await it. 
    - Create temporary quere for response. [One queue + "message id" would be enought, but I decide to go to this way]    
    - Publish _request_ to the "Direct request queue" of another service. Message contains name of temporary queue for answer.
- Service 2
    - Consume _request_ from Service 1.
    - Do the job!
    - Publish _response_ to temporary queue
- Service 1
    - Consume _response_ from temporary queue
    - Destroy temporary queue
    - SetResult to task from TaskCompletionSource
    - "await" relese processing

![Executing synchronous request throught RabbitMQ](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/RabbitSynchronousRequest.puml)


## Change project settings for all services
Сhanging settings for all services occurs using the web page. 
1. Web service syncronuosly gets main information about project from mainbot service. 
2. Web service publishes to central hub request about needed settings infromation for all services and memorize eventId.
3. Web service sends blazor-page with main information about project to user.
4. If the service needs some settings for correct working It pulishes needed infromation with income eventId.
5. Web service consumes "settings messages", regenerates web page for user and sends web page to user through SignalR.

After changing settings User saves information.

6. Because I don't want to extract information from messages for "smart publishing information", I publish all information to CentralHub. 
7. CentralHub sends these messages to each service. 
8. If the service receives a message intended for another service It ignores this message.
9. If the service receives a message intended for itself It proceses this message.

I don't wait for an anwer aobut the successful competition of the update. 

![Request needed setting from all services](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/WebAdminChangeSettingsRequest.puml)
![Update setting in all services](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/WebAdminChangeSettingsUpdate.puml)


# [Underconstraction]

# Some logic of working of Bot

### First telegram message processing 

![Sequence diagram by PlantUML](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/zavpav/TgBotEnterprise/main/ProcessingTelegramMessage.puml)

