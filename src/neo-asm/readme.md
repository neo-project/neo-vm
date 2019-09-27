# NEO-ASML

NEO-ASML 是NEOVM的汇编语言

他看起来像这样

```
Main()
{
    PUSH 1//push 1 number
    PUSH 2
    CALL method1
    RET;
}
method1()
{
label1：
    ADD
88:
    RET
}
```

NEO-ASML 是NEO智能合约编译器的中间件，其它语言的代码可以先编译为NEO-ASML，再调用本模块编译为AVM

# NEO-ASML语法

## 函数

函数以 名字(){} 的形式定义,Main函数将会作为avm的入口点
函数允许在 小括号内插入注释(/**/)

函数名用作 CALL 指令的参数，Main函数除外
```
method01()
{
    PUSH 1
    PUSH 2
    ADD
    RET
}

```

## 注释

支持任意位置的独立行注释 //开头 或者 /**/
```
//hello world
/*
    Hello world
    in 2 lines
*/
```

## 标签

标签用来在函数内部跳转，用作 JMP JMPIF JMPIFNOT 指令的参数
标签可以用数字或字母命名，允许多个同一位置的标签，标签只能存在于函数内部
一个函数内不能JMP到其它函数的标签
```
label1:
23:
0xaa:
```

## 指令

指令有两种形式，一个参数 和没有参数
指令和参数使用空格隔开，指令结尾用换行或者;
比如

```
PUSH 1 //1 param and newline
PUSH 2; //end with ;
ADD // no param
```
除PUSH指令以外，其它的指令名均直接为NEOVM OPCODE的名字

PUSH 指令在编译为AVM时展开

## 指令参数

PUSH指令的参数
PUSH 指令的参数可以是 数字 bytearray bool string
```
PUSH -1
PUSH 0x3344
PUSH [0x44,0x33]
PUSH false
PUSH "hello"
```

JMP JMPIF JMPIFNOT 指令的参数为label的名字，可以用双引号也可以不用
```
JMP label1
JMP "label1"
```
CALL 指令的参数为 method的名字，可以用双引号也可以不用
```
CALL method01
CALL "method01"
```

SYSCALL 指令的参数为API hash地址或者 名字,是名字时必须使用双引号，hash地址支持多种数字表示形式,与PUSH 相同
```
SYSCALL 1113379787
SYSCALL 0x11235322
SYSCALL [0x22,0x53,0x23,0x11]
SYSCALL "Neo.FrameWork.CurrentSome"
```


