Its.Cqrs
========

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/Its.Cqrs?svg=true&branch=master)](https://ci.appveyor.com/project/jonsequitur/its-cqrs) [![Join the chat at https://gitter.im/jonsequitur/Its.Cqrs](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/jonsequitur/Its.Cqrs?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) 

A set of libraries for CQRS and Event Sourcing, with a Domain-Driven Design flavor. 

* Its.Domain: [![NuGet Status](http://img.shields.io/nuget/v/Its.Domain.svg?style=flat)](https://www.nuget.org/packages/Its.Domain/) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Its.Domain.svg)]()
* Its.Domain.Api: [![NuGet Status](http://img.shields.io/nuget/v/Its.Domain.Api.svg?style=flat)](https://www.nuget.org/packages/Its.Domain.Api/) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Its.Domain.Api.svg)]()
* Its.Domain.Sql: [![NuGet Status](http://img.shields.io/nuget/v/Its.Domain.Sql.svg?style=flat)](https://www.nuget.org/packages/Its.Domain.Sql/) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Its.Domain.Sql.svg)]()
* Its.Domain.Testing: [![NuGet Status](http://img.shields.io/nuget/v/Its.Domain.Testing.svg?style=flat)](https://www.nuget.org/packages/Its.Domain.Testing/) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Its.Domain.Testing.svg)]()



Here are some useful definitions:
---------------------------------

[CQRS](http://martinfowler.com/bliki/CQRS.html)

[Domain Driven Design](http://en.wikipedia.org/wiki/Domain-driven_design)

[Event Sourcing](http://martinfowler.com/eaaDev/EventSourcing.html)

Ok, why?
--------

Systems built using these patterns can offer some improvements over the "traditional" approach where a single model, generally stored in a relational database, is used for both reads and writes. These improvements include:

* More cohesive business logic. Commands sent to your domain model are the entry point for all changes to its state. This provides a clear pattern for implementing validation and authorization.
* Better control over concurrency. The source of truth for your domain model is an incrementing event stream, making concurrency easier to detect and respond to in scenario-specific ways, as well as removing the need for pessimistic locks and explicit transactions. 
* Improved scalability. By decoupling reads and writes, the read and write sides of the application can be scaled independently.
* Simplified partitioning. Event streams partition naturally on a single key, and there is no partitioning or replication of related data needed.
* Simplified deployments. Read model databases are projections from one or more event streams. These databases can be treated as disposable, queryable caches. This allows multiple versions of an application with different data schema needs to run at the same time, each with their own read model database.
* Improved auditability. The event stream is append-only, and captures the important steps in the process you've modeled in your domain. This improves diagnosability, allows you to "debug history" when something unexpected happens, and can provide insights into how the system is being used that are lost in traditional systems where data is being overwritten.

Read more [in the wiki](https://github.com/jonsequitur/Its.Cqrs/wiki).
