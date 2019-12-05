
![logo](Tools/logo.png)

A simple actor system inspired by Akka.NET, designed to be simpler, easier to use, more predictable, less overly-engineered, and designed with plugins (AssemblyLoadContext) in mind.

NOT YET FUNCTIONAL (almost there!)  
Critical things left to do:
 - Actor scheduler
 - TCP networking and networking layer
 - Tests
 - NuGet package

The system is largely similar to Akka.NET with several key differences:
 - There is no `Context` and `Tell`ing an actor always requires you to specify the sender. `Context` in Akka.NET is a static class and is tightly coupled with the time that the current actor is running, and will become invalid in callbacks or async contexts. We do away with that here. To alleviate the pain of having to explicitly define the sender actor every time, the base Actor class comes with it's own `Tell(actor, data)` method which sends it's own actor reference behind the scenes. Further, there is a `Respond(data)` method which responds to the sender of the current message, `Tattle(data)` which sends a message to the parent, and `TellChildren(data)` which sends the data to all children.
 - The actor logger and the message stash do not have to be manually added to an actor, they are part of the base Actor class.
 - There is only one base Actor class, which acts like Akka.NET's ReceiveActor.
 - The Tell method is generic and strongly-typed, there is no `Tell(object)` method.
 - The `Ask` method doesn't maliciously trick you into thinking you can do `async` in an actor by returning Task. Caesura.Actor's `Ask` instead returns nothing, and accepts a callback that is properly scheduled along with the actor.
 - There is currently no `BecomeStacked` method, only `Become`. It's so unused and specific that I probably won't add it.
 - Just like in Akka.NET, `async` cannot be used as it messes with the actor scheduler. And like Akka.NET, Caesura.Actors comes with a lot of methods to aleviate this and re-enabled asynchronous programming through callbacks or re-queuing the actor in the scheduler. The difference is that Caesura.Actors makes these methods a lot more direct and coherent and map better to the idea of asynchronous programming rather than just making the scheduler happy. For example, the Actor class has a `Wait` method which replaces `Task.Delay`, which Akka.NET does not have.
 - There is no Bubble Walker, All top-level user-defined actors in the system are a child of the root actor (`/`), and the root has no parent, it's parent points to `Nobody`.
 - You are not annoyingly forced to use an external library and HOCON file just to simply use the system's logging facilities. Just hook a method on to the logger callback. You know, like any normal library.
 - And most importantly, actors can be explicitly destroyed, immedately and permanently, and entirely removed from the system, every last trace of them. This is critical to a plugin system using `AssemblyLoadContext` so the system won't hold on to an assembly, and is why I was forced to write this library.

I love the Akka.NET project, it's a wonderful project, I'm just frustrated :)

## Copyright and license

Caesura.Actors is copyright 2019 Kameko. All images and logos are copyright 2019 Kameko. Source code is licensed under the Microsoft Public License (MS-PL).
