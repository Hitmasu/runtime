// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
class C
{
    private string s = "This is private";
}

class B
{
    public string t = "This is safe";
}

class Class1
{
    public static int Main()
    {
        B[,] ab = new B[1, 1];
        object[,] ao = ab;
        try
        {
            ao[0, 0] = new C();
        }
        catch (ArrayTypeMismatchException)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        Console.WriteLine(ab[0, 0].t);
        Console.WriteLine("FAILED");
        return 1;
    }
}

