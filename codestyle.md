## Code style guide

Before starting working with the source code it is recommended to install [editorconfig.org](http://editorconfig.org) extension to your IDE. This will automatically detect `.editorconfig` file and configure IDE for you each time you launch MSCMP solution. If you don't want to install it and just want to manually configure your IDE follow the settings from `.editorconfig` file to do so.

Few basic code style rules:

### Identation should be using tabs of size 4.

### What should use `PascalCase`.
* Methods and functions (e.g. `public void SetPosition(Vector3 position)`)
* Public fields (e.g. `public Vector3 WorldPosition { get; set; }`)

### What should use `camelCase`.
 * Private fields (e.g. `Vector3 targetInterpolationPosition;`)
 * Local variables and parameters.

### What about brackets?

You put bracket always on the same line as the actual code.

A little snipped showing how brackets should be placed:

```csharp
namespace MSCMP {
  public class MyClass {
    public bool DoSomething(int param) {
    	if (param > 10) {
    		return false;
    	}
    	else {
    		switch (param) {
	    		case 1: {
    				break;
    			}

    			default: {
	    			break;
    			}
    		}
    	}
    	return true;
    }
  }
}

```

### String formatting.

Use string interpolation instead of using `+` operator. Some old parts of code are using `+` operator however this should be changed to string interpolation.

So use:
```csharp
int variable = 10;
SendMessage($"My super number {variable}");
```

Instead of:
```csharp
int variable = 10;
SendMessage("My super number " + variable);
```

### Have any question about code style?

If you have any question. First of all check existing code - if you still don't find answer to your question here you can always ask on the [discord](https://discordapp.com/invite/79B8gKC) of the project.
