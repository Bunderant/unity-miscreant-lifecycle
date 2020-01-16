# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- Check out the Trello page for the most current development info: https://trello.com/b/y00xAcUH

## [0.1.0] - 2020-01-16
### Added
- Inherit the abstract ManagedUpdatesBehaviour to independently toggle Update and FixedUpdate callbacks on components. 
- Associate ManagedUpdatesBehaviour instances with a ManagedExecutionGroup reference, and specify groups' execution order via a ManagedExecutionSystem.
- Call RunUpdate() and RunFixedUpdate() on ManagedExecutionSystem assets to send callbacks to all their subscribers. 
- Subscribing/Unsubscribing to a group happens exclusively through the built-in OnEnable and OnDisable callbacks of ManagedUpdatesBehaviour. 
- Performance is as good or better than Unity's built-in Update and FixedUpdate callbacks (especially so in the Editor). 
- 100% test coverage for the Miscreant.Lifecycle.Runtime assembly. 