# Cycle Breakers

Certain assemblies in WPF contain cycles - they use types from other assemblies which, in turn, use types from them, effectively creating a cycle.

To allow assemblies to compile, we introduced cycle-breaking assemblies which re-define the problematic types (without an implementation and/or closure) and allow projects to referene them instead of referencing themselves, and break the cycle.

There are 2 types of cycle breaking assemblies:

* **Implementation cycle breakers**

    An implementation cycle breaker assembly is referenced directly from a project that uses types from another "cycled" assembly in its implementation. An implemention cycle breaking assembly is likely to include types *and* their closures, because the closure may be used from the calling assembly.

* **API cycle breakers**

    An API cycle breaking assembly is an assembly is likely referenced from a ref-assembly, or an implementation cycle breaking assembly. It is likely to not contain type closures, as much as possible, and generally minimal - way smaller in footprint than an implementation cycle breaker.
    
Examples of such assemblies are under *src\Microsoft.DotNet.Wpf\cycle-breakers*:

>  Ex: PresentationFramework\PresentationFramework-PresentationUI-api-cycle.csproj

In the previous example, this assembly contains types from PresentationFramework and is exposing them to PresentationUI as an api-cycle-breaker. You will likely see minimal types here.


>  Ex: PresentationFramework\PresentationFramework-ReachFramework-impl-cycle.csproj

In the previous example, this assembly contains types from PresentationFramework and is exposing them to the ReachFramework's implementation. This is what we call an implementation cycle-breaker and you will likely see types with full closures here.