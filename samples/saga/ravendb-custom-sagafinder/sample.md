---
title: Custom Saga Finding Logic (RavenDB)
summary: Perform custom saga finding logic based on custom query logic when the Saga storage is RavenDB and how to use multiple Unique attributes.
component: Raven
tags:
 - Saga
 - SagaFinder
 - RavenDB
related:
 - nservicebus/sagas
 - nservicebus/ravendb
---

include: raven-resourcemanagerid-warning


## Code walk-through

When the default Saga message mappings do not satisfy the requirements, custom logic can be put in place to allow NServiceBus to find a saga data instance based on which logic best suites the environment.

This sample shows how to:

 * Perform custom saga finding logic based on custom query logic.
 * Use multiple Unique attributes using the default [RavenDB Unique Constraint bundle](http://ravendb.net/docs/search/latest/csharp?searchTerm=extending%20bundles%20unique-constraints).


### RavenDB setup

This sample requires [RavenDB persistence](/nservicebus/ravendb/) package and a running RavenDB instance configured accordingly.

NServiceBus out of the box does not support saga data with multiple `Unique` attributes, in order to achieve that it is possible to utilize the default RavenDB `UniqueConstraint` Bundle. Follow the [instructions on the RavenDB site](http://ravendb.net/docs/search/latest/csharp?searchTerm=extending%20bundles%20unique-constraints) to correctly install the bundle in the RavenDB server. It is also required to configure the client side of the bundle by registering the `UniqueConstraintsStoreListener` as shown above.

INFO: If running this sample against an external RavenDB server ensure that the `RavenDB.Bundles.UniqueConstraints` [bundle](http://ravendb.net/docs/search/latest/csharp?searchTerm=extending%20bundles%20unique-constraints) is currently installed according to the [extending RavenDB](http://ravendb.net/docs/search/latest/csharp?searchTerm=server%20extending%20plugins) documentation. If the server side of the plugin is not correctly loaded, notice that the [`SagaNotFoundHandler`](/nservicebus/sagas/saga-not-found.md) will be invoked.


### In Process Raven Host

So that no running instance of RavenDB server is required.

snippet:ravenhost


### The Saga

The saga shown in the sample is a very simple order management saga that:

 * handles the creation of an order;
 * offloads the payment process to a different handler;
 * handles the completion of the payment process;
 * completes the order;

snippet:TheSagaRavenDB

From the process point of view it is important to note that the saga is not sending to the payment processor the order id, instead it is sending a payment transaction id. A saga can be correlated given more than one unique attribute, such as `OrderId` and `PaymentTransactionId`, requiring both to be treated as unique.

snippet:OrderSagaDataRavenDB

A properties uniqueness can be expressed by using the `UniqueConstraint` attribute provided by the RavenDB bundle.

At start-up the sample will send a `StartOrder` message, since the saga data class is decorated with custom attributes it is required to also plug custom logic to find a saga data instance:

snippet:CustomSagaFinderWithUniqueConstraintRavenDB

Building a saga finder requires to define a class that implements the `IFindSagas<TSagaData>.Using<TMessage>` interface. The class will be automatically picked up by NServiceBus at configuration time and used each time a message of type `TMessage`, that is expected to load a saga of type `TSagaData`, is received. The `FindBy` method will be invoked by NServiceBus.

NOTE: In the sample the implementation of the `ConfigureHowToFindSaga` method, that is required, is empty because since a saga finder is provided for each message type that the saga is handling. It is not required to provide a saga finder for every message type, a mix of standard saga mappings and custom saga finding is a valid scenario.