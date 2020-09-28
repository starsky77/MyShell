# MiniShell设计开发文档

## 1. 系统概述

### 1.1 程序设计语言与开发环境

本项目通过 .Net Core 3.1，利用C#程序设计语言完成；
由于 .Net Core平台的跨平台能力，能够将程序发布为windows版本或Linux版本，因此在Window环境下完成开发，并发布为Linux-x64版本，并在Linux环境下通过测试。

### 1.2 设计思路

一个简单的Shell的生命周期为如下一个循环：
指令读取→参数切分→指令运行→内存释放→指令读取……

指令运行部分涉及到处理多种不同指令的情况，逻辑相对复杂，因此可以分为以下几个过程：

变量替换（替换带$的变量）→处理批量指令（文件读取批量指令）→若无批量指令则处理单独指令

单独指令的处理可以再分为以下过程：
若存在重定向符号，则以重定向的方式来处理指令；
若存在管道符号，则以管道方式来处理指令；
若均不存在，则按正常情况处理指令；
虽然分为三种情况，但具体实现是借助两个函数来实现的。

根据以上思路，为了更好的程序可读性和扩展性，将程序分为三个文件和三个个类来实现：

三组类和文件分别为**ShellConsole.cs**，实现程序主体，内部程序为**ShellConsole**类，代表shell主体；
**Command.cs**，实现全部的内部指令，内部程序为**Command**类，静态类，用于封装所有的内部指令
**ConsoleHelper.cs**，辅助程序实现带有不同颜色标记的输入输出，内部程序为**ConsoleHelper**类。

具体实现见2.功能模块设计

### 1.3 使用安装说明

程序已经直接输出为Linux-x64版本下的独立程序，不需要安装任何其他配置，在Linux环境下，只需要保证"Clib.so","umask.so","help","MyShell_WZQ","MyShell_WZQ.pdb"处于同一目录下，并通过Linux的终端输入可执行文件MyShell_WZQ即可启动MiniShell。

如果需要编译源代码的话，则需要提前安装 .net core 3.1 的配置。

## 2. 功能模块设计

这里主要讲述在1.2中提及的三个类的实现，其中**ConsoleHelper**类由于内容极为简单，因此在此不再赘述，主要讲述和shell有关的**ShellConsole**类和**Command**类

### 2.1 ShellConsole类

ShellConsole类实现和shell生命周期有关的所有函数，并且定义变量去存储环境变量以及用户定义的变量，以下通过一些重要函数的实现讲解其实现过程

#### 2.1.1 public void MainLoop()

整个shell程序的运行过程都在`MainLoop()`中实现，因此实际上在本程序的主函数只需要进行`shell.MainLoop();`就可以运行。

而`MainLoop()`的过程也就是在1.2中提到的shell的生命周期，由于C#拥有内存自动释放的功能，因此不用实现内存释放，只需要完成以下过程即可：
指令读取→参数切分→指令运行

命令读取后通过正则表达式将指令切分成数个字符串，判断指令是否为空后开始处理指令。

```csharp
public void MainLoop()
{
    //环境变量读取或初始化
    //PWD = HOME = "./";
    PWD = HOME = Environment.CurrentDirectory;
    string path = Environment.GetEnvironmentVariable("PATH");
    _pathVariables.AddRange(path.Split(';'));
    //程序主循环，反复输入与命令处理
    while (true)
    {
        //输出命令提示符
        ConsoleHelper.Write(PWD + ">");
        string line = ConsoleHelper.ReadLine(ConsoleColor.Yellow);
        //通过正则表达式将输入分割为多个字符串
        string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Trim('\"');
        }
        //空指令，跳过
        if (args.Length == 1 && args[0] == "")
            continue;
        //处理指令
        Execute(args);
    }
}
```

### 2.1.2 private void Execute(string[] args)

Execute负责指令的实现，逻辑也在1.2中提及,代码较为简短，主要是组织不同函数的处理顺序，以下通过源代码和注释来说明。

```csharp
private void Execute(string[] args)
{
    if (args[0] == null)
    {
        ConsoleHelper.WriteLine("Error:No command name!", ConsoleColor.Red);
    }
    else
    {
        //将带有$的变量进行替换
        bool existent = HandleQuote(args);
        if(!existent)
        {
            return;
        }
        //处理批量指令
        if (args[0] == "myshell")
        {
            ExecuteFile(args[1], args);
        }
        //单独处理指令
        else
        {
            //当前版本下，同一个指令只能使用重定向和管道中的一个；
            HandleReDir(args);
            HandlePipe(args);
        }

    }
}
```

其中`HandleQuote`的功能是识别`args`中所有带$的参数，并通过存储所有临时变量的键值对`Dictionary<string, string> variables`来将变量名替换为变量值，并且当变量不存在时会产生报错。

### 2.1.3 private void ExecuteFile(string filePath, string[] parameter)

`ExecuteFile`用于处理批量指令，处理批量指令的方式就是在读取文件后，按照原先shell生命周期的方式，去一行一行处理所有指令，因此这一段代码基本与`MainLoop`中的一致。

唯一的不同之处在于，脚本文件的读取可能伴随有参数，这些参数均需要以\$1,\$2等方式来体现，因此在改函数内多出两个函数`HandleArgs`与`ClearArgs`来处理参数，前者是将所有输入的参数从`args`中取出按照\$1,\$2的方式存储在键值对`Dictionary<string, string> variables`中，后者则是清除指令中的全部\$1,\$2的变量。

以下为`ExecuteFile`的代码：

```csharp
private void ExecuteFile(string filePath, string[] parameter)
{
    StreamReader inputStream = new StreamReader(PWD + filePath);
    string line = inputStream.ReadLine();
    //处理脚本参数
    HandleArgs(parameter);
    //逐行处理脚本命令
    while (line != null)
    {
        string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Trim('\"');
        }
        Execute(args);
        line = inputStream.ReadLine();
    }
    //清除脚本参数
    ClearArgs(parameter);
}
```

### 2.1.4 public (StreamReader, bool) handledBuiltin(string[] args)

`handledBuiltin`用于专门处理内部指令，由于在处理指令的过程要首先判断指令是否是内部指令，因此该指令在后面的函数中都较为重要，因此先进行介绍。

函数的返回值为两个，第一个`StreamReader`返回内部指令的返回值，如果不是内部指令或者内部指令没有返回值则为null，第二个布尔变量，代表该指令是否是内部指令。

由于内部指令数量庞大且可能存在扩展的可能性，因此如果采用if-else结果或者switch结果都太过于冗杂，因此本程序采用C#中的**委托**（实质为函数指针），来封装所有内部指令。再通过一个字符串数组存储所有内部指令，近通过一个for循环就可以完成对于所有内部指令的检查，而不需要接触庞大的分支结构，对于新增的内部指令只需要在委托和字符串数组中添加指令即可，可以很方便地实现内部指令的增加。

委托以及字符串数组如下：

```csharp
public delegate StreamReader builtin_fun(string[] args);

//存储所有内部指令的名称
public static string[] builtin_str =
{
    "bg","cd","clr","dir","echo" ,"exec","exit",
    "environ","fg","help","jobs" ,"pwd" ,"quit",
    "set","shift" ,"test","time" ,"umask","unset"
};

//通过C#的委托（即函数指针）来封装所有内部指令的函数
public static builtin_fun[] builtin_com =
{
    Command.bg, Command.cd, Command.clr,Command.dir,Command.echo,
    Command.exec,Command.exit,Command.environ,Command.fg,Command.help,
    Command.jobs,Command.pwd,Command.quit,Command.set,Command.shift,
    Command.test,Command.time,Command.umask,Command.unset
};

//返回内部指令的梳理
public static int builtin_num()
{
    return builtin_str.Length;
}
```

借助委托，内部指令的处理通过如下数行代码就能完成,且一旦完成后不需要因为内部指令的增减而修改。

```csharp
//处理内部指令，返回内部指令的返回值（没有则为null），以及是否具有内部指令
public (StreamReader, bool) handledBuiltin(string[] args)
{
    bool isBuiltinCommand = false;
    StreamReader builtin_stream = null;
    //for 循环来检查所有的内部指令
    for (int i = 0; i < builtin_num(); i++)
    {
        if (args[0] == builtin_str[i])
        {
            builtin_stream = builtin_com[i](args);
            isBuiltinCommand = true;
            break;
        }
    }
    return (builtin_stream, isBuiltinCommand);
}
```


### 2.1.5 private void HandleReDir(string[] args)

`HandleReDir`实际上包含了两个部分，一个是对于一般指令的处理（无重定向和管道），另一个是对于重定向指令的处理。

运行程序主体先判断程序属于哪一种情况:管道、重定向、或一般指令

```csharp
//如果指令中存在管道符号则跳过该函数
int checkPipe = Array.FindIndex(args, x => x == "|");
if (checkPipe > 0)
    return;
int inputReDirIndex, outputReDirIndex;
string InputFile = null, OutputFile = null;

//检测重定向符号并找到输入输出的文件名
inputReDirIndex = Array.FindIndex(args, x => (x == "<"));
if (inputReDirIndex > 0)
    InputFile = args[inputReDirIndex + 1];

outputReDirIndex = Array.FindIndex(args, x => (x == ">" || x == ">>"));
if (outputReDirIndex > 0)
    OutputFile = args[outputReDirIndex + 1];
```

通过上述程序就可以完成判别，如果是管道则直接退出函数，其他情况的话则判断重定向符的位置，并找出重定向的位置;

在处理重定向时需要注意一点，由于重定向符以及重定向的位置不属于指令本身，因此需要将这两个部分找出并抽离指令后传入特定的指令处理函数，否则会导致处理指令时多出参数。因此通过以下两个变量来完成，`minIndex`代表重定向符号最早出现的位置，`partition`则是抽离了重定向参数的指令。

```csharp
//用于分离重定向符号和指令本身
int minIndex = -1;
string[] partition;
```

存在输入重定向，则从指定的文件中读取输入`StreamReader`中，以供之后使用，如果没有输入重定向则`inputStream`为null，并不会影响后期处理

```csharp
if (inputReDirIndex > 0)
{
    inputStream = new StreamReader(PWD + "/" + InputFile);
    minIndex = inputReDirIndex;
}
```

无论是否存在输出重定向，都要先检查指令是否为内部指令,如果不是内部指令则采用外部指令的处理方式

```csharp
bool hasBulitin = false;
(output_sw, hasBulitin) = handledBuiltin(partition);
if (!hasBulitin)
{
    //不是内部指令，进行外部指令查询
    output_sw = LaunchOneProcess(partition, inputStream, true);
}
```

如上方代码所示，输出均会存储于`output_sw`中;则如果存在输出重定向，根据输出重定向的情况进行输出：

```csharp
 if (args[outputReDirIndex] == ">")
{
    //覆盖文件
    using (StreamWriter streamWriter = new StreamWriter(PWD + "/" + OutputFile, false))
    {
        streamWriter.WriteLine(output_str);
    }

}
else if (args[outputReDirIndex] == ">>")
{
    //追加文件
    using (StreamWriter streamWriter = new StreamWriter(PWD + "/" + OutputFile, true))
    {
        streamWriter.WriteLine(output_str);
    }
}
```

如果不存在输出重定向，则将`output_sw`直接输出（为null则不输出）

```csharp
//不存在输出的指令
if (output_sw == null)
{
    return;
}
//有输出的指令
ConsoleHelper.WriteLine(output_sw.ReadToEnd());
```

### 2.1.6 private void HandlePipe(string[] args)

管道指令的处理思路，是通过依次检测管道符号 | 的位置，每次处理下一个 | 之前的指令并存储其输出，再作为输入传入下一个指令，以此循环知道无法找到新的 | 符号为止。

需要注意的是，由于内部指令均不支持输入重定向，因此在参与管道指令时只可能出现在管道的第一条，因此只对第一个指令做内部指令检测。

```csharp
//首先处理第一个指令，先测试其是否为内部指令
partition = new string[foundIndex - start];
Array.Copy(args, start, partition, 0, foundIndex - start);
//如果存在内部指令，则lastOutput为内部指令的输出，否则为null
(lastOutput, hasBulitin) = handledBuiltin(partition);
//存在内部指令，从下一个管道符号开始执行外部指令
if (hasBulitin)
{
    start = foundIndex + 1;
}
```

单独处理内部指令后开始处理外部指令，即上方的思路，代码如下：

```csharp
//处理外部指令
while (start < args.Length)
{
    //不断寻找管道符号
    foundIndex = Array.FindIndex(args, start, x => x == "|");

    if (foundIndex < 0)
    {
        foundIndex = args.Length;
    }

    //将指令单独分出，并执行
    partition = new string[foundIndex - start];
    Array.Copy(args, start, partition, 0, foundIndex - start);

    //进程是否需要重定向
    bool isReturnOutput = true;
    if (foundIndex >= args.Length)
        isReturnOutput = false;

    // PATH
    //调用进程执行外部指令
    lastOutput = LaunchOneProcess(partition, lastOutput, isReturnOutput);

    start = foundIndex + 1;
}
```


### 2.1.7 调用进程的两个函数（处理外部指令）

外部指令乃是不需要再shell内部实现，而是调用外部命令的指令，因此在shell中需要提供调用外部命令的接口，本程序通过以下两个函数来实现这一功能：

```csharp
//调用进程：找到可以执行的命令的位置
private StreamReader LaunchOneProcess(string[] args, StreamReader standardInput, bool isReturnOutput)

//启动进程，在确定命令的位置后，根据需求调整参数来启动
private StreamReader StartProcess(string fullFilePath, string[] args, StreamReader standardInput, bool isReturnOutput)
```

外部指令的位置存在于系统环境变量的PATH内的某一位置中，因此检索外部指令就是遍历所有的PATH内的目录，知道找到可以执行的指令为止。因此采用try-catch结构，当指令无法执行时（即没有找到对应的指令），则重新寻找新的目录来执行，知道所有目录被遍历依然没有发现指令，则说明该指令不存在。

```csharp
private StreamReader LaunchOneProcess(string[] args, StreamReader standardInput, bool isReturnOutput)
`{
    string fullFilePath;
    StreamReader standardOutput = null;

    //首先检查指令是否存在与当前目录下
    fullFilePath = Path.Combine("./", args[0]);
    try
    {
        standardOutput = StartProcess(fullFilePath, args, standardInput, isReturnOutput);
    }
    //指令不存在与当前目录下
    catch (ExternalException)
    {
        //遍历所有PATH变量，查询外部指令
        bool isFound = false;
        
        foreach (string directory in _pathVariables)
        {
            //获取完整路径
            fullFilePath = Path.Combine(directory, args[0]);
            if (isFound)
            {
                return standardOutput;
            }
            try
            {
                standardOutput = StartProcess(fullFilePath, args, standardInput, isReturnOutput);
                isFound = true;
            }
            //出现错误，即该目录下不存在该指令，则跳入下一个目录查询
            catch (ExternalException e)
            {
                //Console.WriteLine("ERROR message:{0}", e.Message);
            }
        }
        //不存在该外部指令
        if (!isFound)
        {
            ConsoleHelper.WriteLine($"[Error] \"{args[0]}\" not found.", ConsoleColor.Red);
            return standardOutput;
        }
    }
    return standardOutput;
}
```

`StartProcess`在于根据需要（是否需要输入重定向/输出重定向）调整process的参数，来启动特定进程，此处的`StartInfo`涉及微软官方文档中process类的参数，关于这些参数的含义以及调用时的注意事项，具体内容可以见[官方文档](https://docs.microsoft.com/zh-cn/dotnet/api/system.diagnostics.process?view=netcore-3.1)

```csharp
 private StreamReader StartProcess(string fullFilePath, string[] args, StreamReader standardInput, bool isReturnOutput)
{
    Process process = new Process();
    //通过StartInfo的调整来启动进程
    process.StartInfo.FileName = fullFilePath;
    bool isFirstToken = true;
    foreach (string arg in args)
    {
        if (isFirstToken)
        {
            isFirstToken = false;
            continue;
        }
        process.StartInfo.ArgumentList.Add(arg);
    }
    //根据微软官方文档，UseShellExecute = false来避免在重定向时报错
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardInput = false;
    //启动输出重定向
    if (isReturnOutput)
        process.StartInfo.RedirectStandardOutput = true;
    //启动输入重定向
    if (standardInput != null)
        process.StartInfo.RedirectStandardInput = true;
    process.Start();
    //将上一个进程的输入传入该进程
    if (standardInput != null)
        process.StandardInput.Write(standardInput.ReadToEnd());

    if (isReturnOutput)
        return process.StandardOutput;

    //等待进程结束
    process.WaitForExit();
    return null;
}
```

### 2.2 Command静态类

静态类是不需要也不能进行实例化的类结构，用于封装所有内部指令恰好合适，因此所有的内部指令都在这个类中完成。

本程序支持从`bg`到`umask`等19种不同的内部指令

由于指令众多，这里不会一一进行说明，对于`time`,`clr`,`pwd`,`quit`,`exit`这些仅仅是显示特定变量，或只需要在C#内简单调用函数就能完成的指令在此不做说明。

#### 2.2.1 public static StreamReader convert_str_stream(string str)

`convert_str_stream`是一个将`string`类型转为 `StreamReader`类型的函数，由于所有内部指令返回值均为`StreamReader`类型所以经常使用。

所有的内部指令内都存在如下一段代码，是为了防止指令调用错误而撰写的，便于在debug时定位问题,以下以cd指令为例：

```csharp
if (args[0] != "cd")
{
    ConsoleHelper.WriteLine("ERROR:The comand is not cd", ConsoleColor.Red);
    return null;
}
```

#### 2.2.2 cd指令

cd指令涉及PWD和HOME这两个环境变量，这二者在程序开始时就初始化为当前的目录（可执行文件所在的目录）。cd指令实质上就是改变PWD变量（因为MiniShell中所有的路径查询都是在PWD的基础上）。

需要特别注意的就是当出现无参数，即单独输入`cd`，或输入`cd .`与`cd ..`的情况，对这几个情况做单独处理，其他情况则直接改变PWD即可。

```csharp
//无参数，回到主目录
if (args.Length == 1)
{
ShellConsole.PWD = ShellConsole.HOME;
}
//过多参数
else if (args.Length > 2)
{
ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
}
else
{
string filePath = null;
//转到当前目录，通过以下方式来保证PWD末尾不会出现"/."
if (args[1] == ".")
{
    filePath = Environment.CurrentDirectory + "/.";
}
//转到父目录，通过以下方式来保证PWD末尾不会出现"/.."
else if (args[1] == "..")
{
    filePath = Environment.CurrentDirectory + "/..";
}
//一般情况
else
{
    filePath = Environment.CurrentDirectory + "/" + args[1];
}
//不存在的目录
if (!Directory.Exists(filePath))
{
    ConsoleHelper.WriteLine("ERROR:The Directory doesn't exist!", ConsoleColor.Red);
}
//确保无误后更改目录
else
{
    Environment.CurrentDirectory = filePath;
    ShellConsole.PWD = Environment.CurrentDirectory;
}
```

#### 2.2.3 dir指令

`dir`指令主要是基于`Directory.GetFiles`来获取特定目录下的文件内容，再逐个输出，主要需要考虑的是没有参数时输出当前目录下的文件，以及存在参数时的情况，整体内容较为简单。

```csharp
string result_str = "";
string [] files = null;
//无参数，显示当前目录
if (args.Length==1)
{
    files = Directory.GetFiles(ShellConsole.PWD);
}
//参数过多
else if(args.Length>2)
{
    ConsoleHelper.WriteLine("ERROR:The comand has too many args", ConsoleColor.Red);
    return null;
}
//有参数，以PWD为基准开始执行命令
else
{
    files = Directory.GetFiles(ShellConsole.PWD + "/" + args[1]);
}
foreach (var file in files)
{
    result_str += file;
    result_str += "\n";
}
return convert_str_stream(result_str);
```

#### 2.2.4 set 与 unset指令

本程序对于set指令的作用作为些微修改，此处的set指令用于创建一个临时变量，以`set (变量名) = （变量值）`的格式创建，随后可以通过$(变量名)的方式来引用变量，而unset指令则是清除一个变量。

`Execute`中的`HandleQuote`函数用来处理变量替换的内容，因此set函数的目的只需要向shell中用于存储临时变量的键值对`Dictionary<string, string> variables`中添加键值对即可，而unset指令则是清除对应的键值对。

需要注意的是对已有变量的重复创建，和不存在的变量的删除均应当作出错误反馈，通过`TryAdd`,可以让重复键值对的创建不产生报错，而只是反馈一个false，从而能够自主控制错误的反馈。`unset`的代码相对较为简单，因此此处展示`set`的具体代码如下：

```csharp
public static StreamReader set(string[] args)
{
    bool success = false;
    if (args[0] != "set")
    {
        ConsoleHelper.WriteLine("ERROR:The comand is not set", ConsoleColor.Red);
        return null;
    }
    else
    {
        //单独输入set指令没有效果
        if (args.Length == 1) { }
        //如果没有第三个参数则将空内容赋值给变量
        else if (args.Length == 2)
        {

            success = ShellConsole.variables.TryAdd(args[1], "");
        }
        //三个以上参数齐全的情况
        else
        {
            //找到赋值号位置
            int label = Array.FindIndex(args, x => x == "=");
            //赋值号不存在则报错
            if (label < 0)
            {
                ConsoleHelper.WriteLine("[ERROR]:Input does not match the format!", ConsoleColor.Red);
            }
            //将赋值号后的内容全部作为变量值传给变量
            else
            {
                string Value = "";
                for (int i = label + 1; i < args.Length; i++)
                {
                    Value += args[i];
                }
                success = ShellConsole.variables.TryAdd(args[1], Value);
            }
        }
    }
    //添加不成功则代表变量已经存在，报错
    if (!success)
    {

        ConsoleHelper.WriteLine("[ERROR]:The variable " + args[1] + " already exists!", ConsoleColor.Red);
    }
    return null;
}
```


#### 2.2.5 shift指令

`shift`指令的作用在于将所有诸如\$1,\$2这样的变量全部向左移一位，即原先\$2的变量值储存在现在的$1中，因此通过按照顺序检索数字变量并将后方变量值赋给前方变量值即可，并对最后一个变量进行删除。

```csharp
bool fir = true;
int lasti = 0;
int i;
//依次查询所有诸如$1,$2的变量，直到变量不存在为止
for (i = 1; ShellConsole.variables.ContainsKey(i.ToString()); i++)
{
    if (fir)
    {
        fir = false;
    }
    else
    {
        //后方变量值赋予前方变量
        ShellConsole.variables[lasti.ToString()] = ShellConsole.variables[i.ToString()];
    }
    //存储上一个变量名
    lasti = i;
}
//移除最后一个变量
if (!fir)
{
    ShellConsole.variables.Remove(i.ToString());
}
```

#### 2.2.6 test指令

`test`指令作为检测特定条件的指令，支持多种检测条件，这里因为时间原因仅实现其中两种作为示例。多种检测条件通过switch结构实现，不同的检测方式通过调用不同的特定函数来完成。

由于在一般的shell中`test`指令要搭配`if`语句或者条件表达式才能体现作用，这里由于没有支持后两者，为了体现`test`的作用，对`test`的结果，通过终端直接输出。

```csharp
//test语句的参数必须为3个：指令+选项+参数
if (args.Length != 3)
{
    ConsoleHelper.WriteLine("[ERROR]:Input does not meet the requirements!", ConsoleColor.Red);
}
else
{
    switch (args[1])
    {
        //判断文件存在
        case "-e":
            //检测文件是否存在或目录是否存在
            if(File.Exists(ShellConsole.PWD+"/"+args[2]) || Directory.Exists(ShellConsole.PWD + "/" + args[2]))
            {
                result = convert_str_stream("The input File/Directory exists!");
            }
            else
            {
                result = convert_str_stream("The input File/Directory doesn't exist!");
            }
            break;

        //判断是否为普通文件
        case "-F":
            if (File.Exists(ShellConsole.PWD + "/" + args[2]))
            {
                result = convert_str_stream("This is a normal file!");
            }
            else
            {
                result = convert_str_stream("This isn't a normal file!");
            }
            break;

        //等待扩充
        default:
            break;
    }
}
```

#### 2.2.7 bg与fg指令

`bg`与`fg`指令，以及后方要提到的`umask`指令，是本程序较为棘手的一个部分。在工程进展后期，本人发现 .net平台并不擅长处理一些较为底层的操作，因此很难实现进程的前后台转换以及掩码的修改。最终必须要借助C语言完成。因此这一部分的代码，是通过在Linux环境下利用gcc编译提前准备好的C语言代码，生成.so动态链接，随后在C#的工程中调用，以此来实现这些功能。

声明引用的动态链接库的函数，在Command类代码的最开始就已经完成。

```csharp
[DllImport("./umask.so", EntryPoint = "umask_C")]
static extern int umask_C(int input);

[DllImport("./CLib.so", EntryPoint = "fg_C")]
static extern int fg_C(int pid);

[DllImport("./CLib.so", EntryPoint = "bg_C")]
static extern int bg_C(int pid);
```

`fg` 将后台任务进程调至前台继续运行，如果后台中有多个任务进程，可以用 fg %num 将选中的任务进程调至前台。

`bg`将挂起的任务进程重新启动运行，如果有多个暂停的任务进程，可以用 bg %num 将选中的任务进程启动运行。

通过调用C语言进程相关的函数，可以较为容易地完成上述功能，其中函数返回1代表执行成功，返回0代表执行失败。

```c
int fg_C(int pid)
{
    setpgid(pid,pid);
    if (tcsetpgrp(1,getpgid(pid))== 0)
    {
        //向对应的进程发送SIG_CONT信号
        kill(pid,SIGCONT);
        //必须使用WUNTRACED
        waitpid(pid,NULL,WUNTRACED);
        return 1;
    }
    else
    {
        //出现错误，返回0
        return 0;
    }
}

int bg_C(int pid)
{
    //发送SIGCONT信号
    if(kill(pid,SIGCONT)<0)
    {   
        //出现错误，返回0
        return 0;
    }
    else
    {
        //必须使用WUNTRACED
        waitpid(pid,NULL,WUNTRACED);
        return 1;
    }
}

```

在C#的程序中，直接调用函数即可，以下以bg指令为例：

```csharp
//进程转到前台
public static StreamReader bg(string[] args)
{
    if (args[0] != "bg")
    {
        ConsoleHelper.WriteLine("ERROR:The comand is not bg", ConsoleColor.Red);
        return null;
    }
    else
    {
        //确保输入格式无误
        if (args.Length > 2)
        {
            ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
        }
        else
        {
            //作业号转为int形式
            int PID = int.Parse(args[1]);
            //调用C语言函数
            int success = bg_C(PID);
            //函数执行不成功，说明作业号不存在
            if (success == 0)
            {
                ConsoleHelper.WriteLine("ERROR:The PID doesn't exist!", ConsoleColor.Red);
            }

        }
    }
    return null;
}

```

#### 2.2.8 umask指令

与2.2.7中的fg和bg指令类似，umask同样通过调用C语言函数可以较为容易地完成。C语言函数`umask()`能够直接将输入作为新的umask值传入，并返回旧的umask值，借助这一函数完成如下C语言代码：

```c
int umask_C(int input)
{
    mode_t new_umask = input;
    mode_t old_umask;
    //无输入，则返回当前umask码
    if (input == 0)
    {
        old_umask = umask(002);
        umask(old_umask);
        return umask(old_umask);
    }
    else
    //有输入则修改umask码
    {
        return umask(new_umask);
    }
}
```

同样，在C#程序中只需要规范格式以及调用函数即可：

```csharp
 //没有参数输入，则直接返回当前umask码
if (args.Length == 1)
{
    int result_int = umask_C(0);
    string result_str = result_int.ToString();
    return convert_str_stream(result_str);
}
//输入格式错误
else if (args.Length != 2 || args[1].Length != 4)
{
    ConsoleHelper.WriteLine("[ERROR]:Input does not match the format!", ConsoleColor.Red);
}
//有参数输入则改变当前umask码
else
{
    int input_int = int.Parse(args[1]);
    umask_C(input_int);
}
```

## 3. 测试结果与截图


1. 启动：输入可执行文件`MyShell`后，在Linux环境下启动该程序：并显示命令提示符（当前路径名称）

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test1.png)


2. 单一外部指令效果：以`ls`,`ps`,`file`为例

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test2.png)


3. 单一内部指令：cd指令

下图分别测试了跳转至父目录，跳转至当前目录（不跳转），跳转至特定目录，跳转至不存在的目录的效果：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test7.png)

4. 单一内部指令：echo指令

echo 后的参数如果由""包裹起来，则其中的空格全部不会忽略；
若没有被""包裹起来，则所有多余的空格会被清除

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test8.png)

5. 单一内部指令：set与unset指令

此处展示了设置变量，引用变量并显示，清除变量，显示不存在的变量的情况

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test9.png)

6. 单一内部指令：clr指令

调用clr指令前：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test10.png)

调用clr指令后：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test11.png)

7. 单一内部指令：time指令

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test12.png)

8. 单一内部指令：dir指令

分别展示无参数的dir指令直接输出本目录下的文件内容，以及由参数的情况下输出特定文件夹内的文件内容

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test14.png)

9. 单一内部指令：help指令以及通过more命令过滤

在可执行文件所在目录下执行help指令，将直接显示全部的用户手册
![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test15.png)

同样可以通过more命令来过滤help用户手册
![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test16.png)

10. 单一内部指令：pwd指令

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test17.png)

11. 批量读取指令并执行

提前准备一段脚本指令：Command1

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test18.png)

执行该脚本文件，输出直接显示在终端中

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test19.png)

12. 批量读取指令并传入参数，且使用shift指令

准备如下的脚本文件：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test20.png)

执行脚本文件并依次传入参数

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test21.png)

可以看到在shift指令的作用下，即使每次输出的是\$1，也依然输出了后方的参数

13. 外部指令与重定向：所有的外部指令均能支持重定向，此处以ps指令为例

输出重定向>：以ps指令输出重定向为例

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test3.png)

输出后检查当前文件夹，找到对应的输出文件

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test4.png)

输出重定向>>：再次执行上方指令

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test6.png)

可以发现，原先的文件下方多出了新内容，而不是被覆盖掉：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test5.png)

输入重定向<:准备一个输入文件

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test24.png)

通过grep指令加输入重定向，查询其中带有echo的部分

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test25.png)

程序功能正常


14. 内部指令与重定向：以echo指令为例，演示内部指令的输出重定向：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test22.png)

检查输出文件的内容，发现echo指令的内容被成功重定向

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test23.png)

15. 管道指令

通过以下指令测试管道的执行效果：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test26.png)

其中command1文件的内容如下：

![test1](C:/Users/wzq/Documents/GitHub/MyShell/img/test24.png)

可以看到cat指令的结果确实传给了grep指令的输入，说明管道指令工作正常。