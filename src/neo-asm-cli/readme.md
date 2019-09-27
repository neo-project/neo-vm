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


# 如何使用

使用命令行调用

```
neo-asm -i mycode.asml -i mycode2.asml -o mycode.avm
```

允许将代码编写在多个asml文件中
-i 输入文件
-o 输出文件

会得到输出 mycode.avm 和  mycode.asmmap
