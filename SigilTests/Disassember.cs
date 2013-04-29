﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection.Emit;
using System.Linq;

namespace SigilTests
{
    [TestClass]
    public class Disassember
    {
        [TestMethod]
        public void Simple()
        {
            Func<int, int, int> d1 = (a, b) => a + b;

            var ops = Sigil.Disassembler<Func<int, int, int>>.Disassemble(d1);

            Assert.IsNotNull(ops);
            Assert.AreEqual(OpCodes.Ldarg_0, ops[0].OpCode);
            Assert.AreEqual(OpCodes.Ldarg_1, ops[1].OpCode);
            Assert.AreEqual(OpCodes.Add, ops[2].OpCode);
            Assert.AreEqual(OpCodes.Ret, ops[3].OpCode);

            var recompiled = ops.EmitAll();
            Assert.IsNotNull(recompiled);

            string instrs;
            var r1 = recompiled.CreateDelegate(out instrs);
            Assert.IsNotNull(r1);

            for (var i = 0; i < 200; i++)
            {
                for (var j = 0; j < 200; j++)
                {
                    Assert.AreEqual(d1(i, j), r1(i, j));
                }
            }

            Assert.AreEqual("ldarg.0\r\nldarg.1\r\nadd\r\nret\r\n", instrs);
        }

        class _Volatile
        {
            public volatile int Foo;
        }

        [TestMethod]
        public void Volatile()
        {
            Action<_Volatile, int> d1 = (a, b) => { var x = a.Foo; x += b; a.Foo = b; };

            var ops = Sigil.Disassembler<Action<_Volatile, int>>.Disassemble(d1);

            var loadField = ops[1];
            Assert.AreEqual(OpCodes.Ldfld, loadField.OpCode);
            Assert.AreEqual(typeof(_Volatile).GetField("Foo"), loadField.Parameters.ElementAt(0));
            Assert.AreEqual(true, loadField.Parameters.ElementAt(1));

            var e1 = ops.EmitAll();
            string instrs;
            var r1 = e1.CreateDelegate(out instrs);

            var v1 = new _Volatile();
            var v2 = new _Volatile();

            for (var i = 0; i < 100; i++)
            {
                d1(v1, i);
                r1(v2, i);

                Assert.AreEqual(v1.Foo, v2.Foo);
            }

            Assert.AreEqual("ldarg.0\r\nvolatile.ldfld Int32 Foo\r\nstloc.0\r\nldloc.0\r\nldarg.1\r\nadd\r\nstloc.0\r\nldarg.0\r\nldarg.1\r\nvolatile.stfld Int32 Foo\r\nret\r\n", instrs);
        }

        [TestMethod]
        public void Branch()
        {
            Func<int, int, int> d1 = (a, b) =>
            {
                if (a == 0) return -1;
                if (b == 0) return -1;

                var c = a * b;

                if (c % 2 == 0) return 1;
                
                return 0;
            };

            var ops = Sigil.Disassembler<Func<int, int, int>>.Disassemble(d1);
            Assert.IsNotNull(ops);

            var e1 = ops.EmitAll();

            string instrs;
            var r1 = e1.CreateDelegate(out instrs);

            for (var i = 0; i < 100; i++)
            {
                for (var j = 0; j < 100; j++)
                {
                    Assert.AreEqual(d1(i, j), r1(i, j));
                }
            }

            Assert.AreEqual("ldarg.0\r\nbrtrue.s _label6\r\nldc.i4.m1\r\nret\r\n\r\n_label6:\r\nldarg.1\r\nbrtrue.s _label11\r\nldc.i4.m1\r\nret\r\n\r\n_label11:\r\nldarg.0\r\nldarg.1\r\nmul\r\nstloc.0\r\nldloc.0\r\nldc.i4.2\r\nrem\r\nbrtrue.s _label22\r\nldc.i4.1\r\nret\r\n\r\n_label22:\r\nldc.i4.0\r\nret\r\n", instrs);
        }
    }
}
