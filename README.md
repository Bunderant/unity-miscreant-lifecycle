# Managed Execution System

(Disclaimer: this project is in-development, so beware if using in a production environment. That said, as of v0.1.0, the runtime assembly has 100% test coverage, and I'll be making sure that's the case for any future releases as well.)

This is a simple wrapper for Unity's built-in MonoBehaviour Update and FixedUpdate callbacks. It primarily allows for more granular control over execution order, and performs as well or better than Unity's built-in update system. It is designed to feel like a natural part of Unity, and subscribe/unsubscribe operations are done exclusively through MonoBehaviour's OnEnable/OnDisable callbacks. Under the hood, the system is built using intrusive linked lists, just as Unity's is internally, so components can safely destroy themselves and each other while the callback lists are being traversed. 

## A note about DOTS

While there are many ways to safely utilize ECS, the Burst compiler, and the Jobs system right away, they still have a little ways to go before they are out of preview. In the interim, this may provide a bit of control and debug functionality in a more "traditional" way of doing things in Unity, especially if you've investigated DOTS for your current needs and it hasn't proved feasible. 

## Who is this for?

This will be a good fit if your project requires different execution orders for groups of Components of the same type, or if you'd like to be able to independently toggle Update and FixedUpdate calls either for components or entire groups. 

Each instance of a Component must have an execution group assigned (implemented as ScriptableObject assets). Each "system" (also a ScriptableObject implementation) provides a reorderable list inspector to specify the execution order of the groups it will iterate over. In Play Mode, you'll have a live view of all Components registered with the system. 

Component configuration is implemented via a custom property drawer, so it will be easy to preserve its functionality if you're migrating existing components with custom inspectors to utilize this system. 

## Supported Unity Versions

As of now, this requires at least Unity 2019.3.0. However, I'll be looking into supporting any earlier versions that also support assembly definitions. 

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

Now, all of the tests for this project should appear in the Test Runner window. Be sure to hit the "clear results" button before each run to make sure the tests are actually running in your builds. 