# Managed Update System

(Disclaimer: this project is in-development, so beware if using in a production environment. That said, as of v0.1.0, the runtime assembly has 100% test coverage, and I'll be making sure that's the case for any future releases as well.)

This is a simple wrapper for Unity's built-in MonoBehaviour Update and FixedUpdate callbacks. It primarily allows for more granular control over execution order, and comes with performance benefits for large groups of objects. It is designed to feel like a natural part of Unity, and subscribe/unsubscribe operations to the update manager are done exclusively through MonoBehaviour's OnEnable/OnDisable callbacks. Under the hood, the system is built using intrusive linked lists, just as Unity's is internally. 

## Who is this for?

Given the emergence of Unity's "Data Oriented Tech Stack" (DOTS), that's a very valid question. While there are many ways to safely utilize ECS, the Burst compiler, and the Jobs system right away, they still have a long way to go before they are out of preview. In the interim, this may provide a bit of convenience and performance for you in a more "traditional" way of doing things in Unity, especially if you've investigated DOTS for your current needs and it hasn't proved feasible. 

If you have an existing project with thousands of updating components, this may be a good fit if your application's performance is CPU bound and you've found yourself micro-optimizing to shave fractions of milliseconds off your frame time. 

This may also be a good fit if your project requires different execution orders for groups of Components of the same type. Each instance of a Component must have an execution group assigned, which are implemented as ScriptableObjects. The system provides a reorderable list inspector to specify the execution order of the groups it will process.  

## Supported Unity Versions

As of now, this requires at least Unity 2019.3.0. Given the likely use case of older projects benefiting from this, however, I'll be looking into supporting any earlier versions that also support assembly definitions. 

## Installation

To add this to your Unity project, copy the repo's URL (https://github.com/Bunderant/unity-miscreant-lifecycle.git) and add it via the '+' icon in Unity's Package Manager window. 

## Testing

If you'd like to use the Test Runner to verify basic functionality on your target platforms, navigate to your project's **Packages** directory (same level as **Assets**), then open **manifest.json**. After the `dependencies` object, insert a `testables` array containing `com.miscreant.lifecycle`. Your manifest should look something like this:

```
{
  "dependencies": {
    "com.unity.some-package": "1.0.0",
    "com.unity.other-package": "2.0.0",
    "com.unity.yet-another-package": "3.0.0",
  },
  "testables": [
    "com.maybe.some.testables.were.already.here",
    "com.miscreant.lifecycle",
  ]
}
```

Now, all of the tests for this project should appear in the Test Runner window.

As of version 2.0.3-preview, Unity's Performance Testing Extension (a dependency in this project) has some reflection-related assembly stripping bugs. To run performance tests in builds, you'll need to prevent stripping of the associated assemblies via your project's **link.xml** file. If you don't have one, create it under your **Assets** directory, then add this: 

```
<linker>
	<assembly fullname="Unity.PerformanceTesting" preserve="all"/>
	<assembly fullname="Newtonsoft.Json" preserve="all"/>
</linker>
```

Now, you should be able to run performance tests in your builds. For more information on Unity's performance testing framework, [take a look here](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@2.0/manual/index.html "Official Unity Docs").