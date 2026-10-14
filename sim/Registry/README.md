# Registry

Every module of one world, wired through their constructors and listed in run order (`MODULES` in the
TypeScript sim). A new module adds a property, its construction after its dependencies, and its place
in `All`. Tests, the network and the game build worlds from a `new Registry()`: a world gets one of
its own, as modules hold its state.
