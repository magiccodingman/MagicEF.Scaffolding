### **Feature Comparison Across Languages**

| Feature                                          | C#  | Java | C/C++ | Swift | Rust | Go  |
| ------------------------------------------------ | --- | ---- | ----- | ----- | ---- | --- |
| **Extension Classes**                            | ✅   | ✅    | ✅     | ✅     | ❌    | ❌   |
| **Partial Classes**                              | ✅   | ❌    | ⚠️    | ⚠️    | ⚠️   | ❌   |
| **Method & Property Hiding (`new`)**             | ✅   | ❌    | ✅     | ❌     | ❌    | ❌   |
| **Attributes (`[Attribute]`, `@Annotation`)**    | ✅   | ✅    | ❌     | ✅     | ✅    | ⚠️  |
| **Metadata (Reflection & Type Inspection)**      | ✅   | ✅    | ❌     | ✅     | ❌    | ⚠️  |
| **Interfaces**                                   | ✅   | ✅    | ✅     | ✅     | ✅    | ✅   |
| **Implicit Conversions (`implicit operator`)**   | ✅   | ❌    | ✅     | ❌     | ❌    | ❌   |
| **Concrete Class Constructors**                  | ✅   | ✅    | ✅     | ✅     | ✅    | ✅   |
| **Override Constructors**                        | ✅   | ✅    | ✅     | ✅     | ❌    | ❌   |
| **Reflection on `T` (Generic Type Reflection)**  | ✅   | ✅    | ❌     | ❌     | ❌    | ⚠️  |
| **Type Inspection (`typeof(x)`, `x.GetType()`)** | ✅   | ✅    | ⚠️    | ✅     | ✅    | ✅   |
| **Custom Getters & Setters (`get { } set { }`)** | ✅   | ❌    | ✅     | ✅     | ⚠️   | ❌   |

---

### **Legend & Notes**

- **✅** = Fully supported
- **❌** = Not supported
- **⚠️** = Partial or limited support

#### **⚠️ Partial Support Explanations**

- **Partial Classes**
    - **C++**: `.h/.cpp` file splitting achieves similar modularity.
    - **Swift**: `extension` allows adding methods but not variables.
    - **Rust**: `impl` blocks allow modular behavior splitting.
- **Attributes / Annotations**
    - **Go**: No built-in system, but struct tags provide a limited alternative.
- **Metadata & Reflection**
    - **C++**: No built-in reflection; RTTI provides limited type info.
    - **Go**: `reflect` package offers some metadata but lacks full introspection.
- **Reflection on `T` (Generic Type Reflection)**
    - **Go**: Can retrieve type names but cannot inspect properties/methods.
- **Type Inspection (`typeof(x)`, `x.GetType()`)**
    - **C++**: `typeid(x).name()` provides some type info, but not full metadata.
- **Custom Getters & Setters**
    - **Rust**: Uses `fn get_x()` and `fn set_x()` instead of native property syntax.
