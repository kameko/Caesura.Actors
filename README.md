
# Caesura.Actors

A simple actor system inspired by Akka.NET, designed to be simpler, easier to use, more predictable, less overly-engineered, and designed with plugins (AssemblyLoadContext) in mind.

NOT YET FUNCTIONAL (almost there!)  
Critical things left to do:
 - Actor scheduler
 - TCP networking and networking layer
 - Tests
 - NuGet package

The system is largely similar to Akka.NET with a few key differences:
 - There is no `Context` and `Tell`ing an actor always requires you to specify the sender. `Context` in Akka.NET is a static class and is tightly coupled with the time that the current actor is running, and will become invalid in callbacks or async contexts. We do away with that here. To alleviate the pain of having to explicitly define the sender actor every time, the base Actor class comes with it's own `Tell(actor, data)` method which sends own actor reference behind the scenes. Further, there is a `Respond(data)` method which responds to the sender of the current message, `Tattle(data)` which sends a message to the parent, and `TellChildren(data)` which sends the data to all children.
 - The actor logger and the message stash do not have to be manually added to an actor, they are part of the base Actor class.
 - There is only one base Actor class, which acts like Akka.NET's ReceiveActor.
 - The Tell method is generic and strongly-typed, there is no `Tell(object)` method.
 - The `Ask` method doesn't maliciously trick you into thinking you can do `async` in an actor by returning Task. Caesura.Actor's `Ask` instead returns nothing, and accepts a callback that is properly scheduled along with the actor.
 - There is currently no `BecomeStacked` method, only `Become`. It's so unused and specific that I probably won't add it.
 - Just like in Akka.NET, `async` cannot be used as it messes with the actor scheduler. And like Akka.NET, Caesura.Actors comes with a lot of methods to aleviate this and re-enabled asynchronous programming through callbacks or re-queuing the actor in the scheduler. The difference is that Caesura.Actors makes these methods a lot more direct and coherent and map better to the idea of asynchronous programming rather than just making the scheduler happy. For example, the Actor class has a `Wait` method which replaces `Task.Delay`, which Akka.NET does not have.
 - And most importantly, actors can be explicitly destroyed, immedately and permanently, and entirely removed from the system, every last trace of them. This is critical to a plugin system using `AssemblyLoadContext` so the system won't hold on to an assembly, and is why I was forced to write this library.

## Copyright and license

Caesura.Actors is copyright 2019 Kameko. Caesura.Actors is licensed under the Microsoft Public License (MS-PL).

## Usage

This is a guide to using Caesura.Actors that will attempt to be concise, to-the-point, and attractive to both people used to Akka.NET or to people who want to use this library that never have before.

An actor is an asynchronous event-driven object that communicates exclusively through read-only messages. It does not directly share it's state with any other actor, nor does any other actor share their state with it. This means actors are always thread-safe despite being a highly-parallel system.

Actors form an actor hierarchy where every actor has a parent-child relationship. Every actor has one parent and can have as many children as it needs. If an actor encounters an unhandled exception, the actor and all it's children are killed and the exception is sent as a message to it's parent.

A single actor is very simple. To create an actor, perform the following steps:
 - Create an `ActorSystem`
 - Create a class that inherits from the base abstract class `Actor`
 - Create an `ActorSchematic` containing a callback on how to create your actor
 - Tell the actor system to create your actor

And that's it!

```cs
using Caesura.Actors;

public class Program
{
    public static void Main(string[] args)
    {
        // turn on the default console logger for the example
        ActorLogger.DefaultLoggerEnabled = true;
        
        var system = ActorSystem.Create("my-system");
        var schematic = new ActorSchematic(() => new MyActor());
        
        // now the actor is running in the system, and the system
        // returned an IActorReference.
        // All actors have a logical path, this one is located at
        // caesura://my-system/my-actor
        var actor = system.NewActor(schematic, "my-actor");
        
        actor.Tell("Hello!", ActorReferences.NoSender);
        
        // Give the system to process the message. Otherwise
        // we would call system.WaitForSystemShutdown()
        // to keep our program open.
        var _ = Console.ReadLine();
    }
}

public class MyActor : Actor
{
    
}
```

This actor isn't very useful though, as it doesn't do anything. We need to give it a behavior.

```cs
public class MyActor : Actor
{
    public MyActor()
    {
        Become(MyBehavior);
    }
    
    private void MyBehavior()
    {
        Receive<string>(
            can_handle: msg => msg == "Hello!",
            handler: msg =>
            {
                ActorLog.Info($"I got a message! {msg}");
                
                Respond("Hi!");
            }
        );
        
        ReceiveAny(
            msg =>
            {
                Respond("I don't know that message kind!");
            }
        );
    }
}
```

The actor will now print `I got a message! Hello!`. The actor also attempts to message the sender of the message back, but because we passed `NoSender`, nothing will happen. The actor also set up the `ReceiveAny` method which will accept any message if no other `Receive<T>` method handled the message. If there is no `ReceiveAny` method and the actor doesn't handle the message in one of it's `Receive<T>` methods, the message will be considered "lost", and the system will log the fact that the message wasn't handled by it's intended recipient.

`Become` can be called multiple times. Each time it will clear every `Receive` method from the actor's behavior so that new `Receive` methods can add to the actor's behavior. This can be used to create a state machine. An example of an actor refusing to cooperate if it was given a message it doesn't understand:

```cs
public class MyActor : Actor
{
    public MyActor()
    {
        Become(MyBehavior);
    }
    
    private void MyBehavior()
    {
        Receive<string>(
            can_handle: msg => msg == "Hello!",
            handler: msg =>
            {
                ActorLog.Info($"I got a message! {msg}");
                
                Respond("Hi!");
            }
        );
        
        ReceiveAny(
            msg =>
            {
                Respond("I don't know that message kind!");
                
                Become(BadBehavior);
            }
        );
    }
    
    private void BadBehavior()
    {
        ReceiveAny(
            msg =>
            {
                Respond("I don't want to talk to you anymore");
            }
        );
    }
}
```

Now the actor will refuse to communicate meaningfully. The old `Receive<string>` and `ReceiveAny` methods in `MyBehavior` are no longer operating because the new `Become(BadBehavior)` call has overridden them.

I'll have to flesh out this tutorial when the system is more buffed out.
