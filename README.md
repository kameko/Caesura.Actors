
![logo](Tools/logo.png)

Caesura.Actors is an actor framework for creating a straight-forward and configurable actor system. It is primarily targeted at providing the actor paradigm as a programming practice rather than as a microservice framework.

This project was created for [Solace](https://github.com/kameko/Solace).

Critical things left to do:
 - TCP networking and networking layer
 - Tests
 - NuGet package

The system is largely similar to Akka.NET with several key differences:
 - There are no `Receive` methods, instead you create a new `Handler` object which natively and transparently supports both synchronous and asynchronous methods through operator overloading. The Handler object has a lot more options and configurability than Akka's Receive does.
 - The actor logger and the message stash do not have to be manually added to an actor, they are part of the base Actor class.
 - There is only one base Actor class, which acts like Akka.NET's ReceiveActor.
 - The Tell method is generic and strongly-typed, there is no `Tell(object)` method.
 - You are not annoyingly forced to use an external library and HOCON file just to simply use the system's logging facilities. Just hook a method on to the logger callback. You know, like any normal library.
 - And most importantly, actors can be explicitly destroyed, immedately and permanently, and entirely removed from the system. This is critical to a plugin system using `AssemblyLoadContext` so the system won't hold on to an assembly, and is why I was forced to write this library.

I love Akka.NET, it's a wonderful project, I'm just frustrated :)

## Copyright and license

Caesura.Actors is copyright 2019 Kameko. All images and logos are copyright 2019 Kameko. Source code is licensed under the Microsoft Public License (MS-PL).
