# teletype-crdt-csharp

This is a C# port of the original [teletype-crdt](https://github.com/atom/teletype-crdt) implementation.
Most of it is a direct translation from JavaScript to C#. I defined a proper object model, but since original implementation
used to add properties on the fly - resulting classes don't provide perfect encapsulation.
There is a lot of room for improvement to make it follow basic SOLID principles, but this is a good baseline that works.

Most of the original test suite is included as well to validate basic functionality.

## Working with the code

Clone the repo and open ```Teletype.sln``` solution file in Visual Studio.

 - ```Teletype``` is an actual library project that defines ```Document``` class. Project is targeting .NET Standard 2.0, so can be used cross-platform in .NET Core.
 - ```Teletype.Tests``` is an unit tests project. It targets .NET Framework 4.7.2, but you should be able to retarget it to .NET Core in order to run on Mac/Linux, if needed.

Typical scenario is to have an instance of ```Document``` class in the background and pass it all the changes made in the front-end editor
through ```SetTextInRange(..)```. As a result you will have a collection of so-called _operations_ that can be shared with other sites.
Every other site will have its own instance of the ```Document``` class (aka _replica_) and will integrate remote operations by calling ```IntegrateOperations(..)```.
From ```IntegrateOperations(..)``` you will get simple linear text changes that can be applied to the front-end editor, therefore bringing remote
remote modification to the local front-end editor at a given site.

Cursor/selection sharing can be implemented by means of _markers_, that are maintained in multiple layers per site. So the typical structure for markers
looks like ```{ SiteId, { LayerId, { MarkerId, <marker-info> } } }```, where all of the IDs are just sequential numbers (1, 2, 3, ...).

It also supports native ```Undo()``` and ```Redo()```, although you are not obligated to use them. Adding/removing text directly through ```SetTextInRange(..)```
will work just fine, although native undo/redo might be more efficient, as it translates into a special operation that does not require to send the inserted
text fragment every time you redo.

Refer to unit tests for more examples.

## TODO

There is no serialization logic currently, so if you need to interact with the original JavaScript or other implementations you will need to add serialization.
Luckily original implementation is using [protobuf](https://github.com/protocolbuffers/protobuf), so it should be fairly easy to generate the contracts for
C# and stitch them with data structures defined in this project.