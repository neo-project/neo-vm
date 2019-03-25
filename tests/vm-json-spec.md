# Overview

This document describes the JSON convention used to describe the VM Test vectors.

# Conventions
The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://www.ietf.org/rfc/rfc2119.txt).

The test vectors utilize JSON and therefore use the same type system (see [RFC 4627](https://www.ietf.org/rfc/rfc4627.txt)). JSON can represent four primitive types (Strings, Numbers, Booleans, and Null) and two structured types (Objects and Arrays). The term "Primitive" in this specification references any of those four primitive JSON types. The term "Structured" references either of the structured JSON types. Whenever this document refers to any JSON type, the first letter is always capitalized: Object, Array, String, Number, Boolean, Null. True and False are also capitalized.

All member names are case sensitive.

# Test suite object
A Test suite object describes the global test information. The Test suite object has the following members:

### category
A String specifying a test category.

This member is REQUIRED.

### name
A String specifying the sub category.

This member is REQUIRED.

### tests
An Array that MUST only hold [Test case](#Test-case-object) objects. This array MAY be empty.

This member is REQUIRED.

# Test case object
A Test case object describes a single or set of interactions to be performed by the virtual machine. The Test case object has the following members:

### name
A String specifying a self explanatory description of the test case.

This member is REQUIRED.

### message # validate name
TODO: proper description for script container.

This member is OPTIONAL. 

This member MAY start with `0x` and MUST BE followed by a sequence of hexadecimal characters.

### script
A String describing the calling opcodes to be executed by the VM.

This member is OPTIONAL. 

This member MAY start with `0x` and MUST BE followed by a sequence of hexadecimal characters.

### scripttable # validate name
TODO: proper description for script table. I expect these are the smart contracts that can be invoked against.

This member is OPTIONAL. 

This member MAY start with `0x` and MUST BE followed by a sequence of hexadecimal characters.

### steps
An Array that MUST only hold [Step](#step-object) objects. This array MAY be empty.

This member is REQUIRED.

# Step object
A Step object describes exactly one set of inputs and interactions for the virtual machine as well as the expected results. The Step object has the following members:

### actions
An Array that MUST be one or more of "StepInto", "StepOut", "StepOver" or "Execute".

This member is REQUIRED.

### result
A [Result](#result-object) object.

This member is REQUIRED.

# Result object
A Result object describes the collective state of the Virtual Machine after all actions in a `Step` have been executed. The Result object has the following members:

### state
A String that MUST be one of "None", "Halt", "Break" or "Fault".

This member describes the state the virtual machine is in.

This member is REQUIRED.

### invocationStack
An Array that MUST only hold [ExecutionContext](#executioncontext) objects. This array MAY be empty.

This member is OPTIONAL.

### resultStack
An Array that MUST only hold [StackItem](#stackitem) objects. This array MAY be empty.

This member is OPTIONAL.

Either the `invocationStack` member or `resultStack` member MUST be included, but both members MUST NOT co-exist in a [Result](#result-object) object.

# ExecutionContext
An ExecutionContext object describes the state of the context. The ExecutionContext object has the following members:

### scriptHash
A String that MAY start with `0x` and MUST BE followed by a sequence of hexadecimal characters.

This member is REQUIRED.

### instructionPointer
A Number describing the count of opcodes that have been executed.  #TODO better wording? It's not really an IP like in any other architecture

This member is REQUIRED.

### nextInstruction
An String describing the human readable name of the next opcode to be executed.

This member is REQUIRED.

### evaluationStack
An Array that MUST only hold [StackItem](#stackitem) objects. This array MAY be empty.

This member is OPTIONAL.

### altStack
An Array that MUST only hold [StackItem](#stackitem) objects. This array MAY be empty.

This member is OPTIONAL.

# Result Stack
An Array that MUST only hold [StackItem](#stackitem) objects. This array MAY be empty.

This member is OPTIONAL.

# StackItem
A StackItem object describes a value of a specific type that can be pushed or popped of the various stacks (evaluationStack, resultStack, altStack). A StackItem object has the following members:

### type
An String that MUST be one of "Array", "Boolean", "ByteArray", "Integer", "Interop", "Map" or "Struct".

This member is REQUIRED.


### value
The value member type depends on the specific VM type specified in the `type` member. 

This member is REQUIRED.

* The "Array", "Map" and "Struct" VM types MUST hold an Array of [StackItem](#stackitem) objects. This array MAY be empty.

* The "Boolean" VM Type MUST BE either `true` or `false`.

* The "ByteArray" VM Type MUST BE a String starting with `0x` followed by a sequence of hexadecimal numbers.

* The "Integer" VM Type MUST BE a Number or a String representation of a decimal number.

* The "Interop" VM Type MUST BE a String with UTF-8 encoding describing the interop call to make.

