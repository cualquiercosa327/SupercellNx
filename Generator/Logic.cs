using System;
using System.Diagnostics;
using System.Linq;
using MoreLinq;

namespace Generator {
	public class Logic : Builtin {
		public override void Define() {
			Statement("=", list => list[2].Type?.AsRuntime(list.AnyRuntime) ?? throw new NotImplementedException(),
				(c, list) => {
					if(list[1] is PList sub)
						switch(sub[0]) {
							case PName("gpr32"):
								c += $"W[(int) {GenerateExpression(sub[1])}] = (uint) ({GenerateExpression(list[2])});";
								return;
							case PName("gpr-or-sp32"):
								c += $"if({GenerateExpression(sub[1])} == 31)";
								c++;
								c += $"SP = (ulong) (uint) ({GenerateExpression(list[2])});";
								c--;
								c += "else";
								c++;
								c += $"W[(int) {GenerateExpression(sub[1])}] = (uint) ({GenerateExpression(list[2])});";
								c--;
								return;
							case PName("gpr64"):
								c += $"(&State->X0)[(int) {GenerateExpression(sub[1])}] = {GenerateExpression(list[2])};";
								return;
							case PName("gpr-or-sp64"):
								c += $"if({GenerateExpression(sub[1])} == 31)";
								c++;
								c += $"SP = {GenerateExpression(list[2])};";
								c--;
								c += "else";
								c++;
								c += $"(&State->X0)[(int) {GenerateExpression(sub[1])}] = {GenerateExpression(list[2])};";
								c--;
								return;
							
							case PName("vec-b"):
								c += $"(&State->V0)[(int) ({GenerateExpression(sub[1])})] = new Vector128<byte>().WithElement(0, {GenerateExpression(list[2])}).As<byte, float>();";
								return;
							case PName("vec-h"):
								c += $"(&State->V0)[(int) ({GenerateExpression(sub[1])})] = new Vector128<ushort>().WithElement(0, {GenerateExpression(list[2])}).As<ushort, float>();";
								return;
							case PName("vec-s"):
								c += $"(&State->V0)[(int) ({GenerateExpression(sub[1])})] = new Vector128<float>().WithElement(0, {GenerateExpression(list[2])});";
								return;
							case PName("vec-d"):
								c += $"(&State->V0)[(int) ({GenerateExpression(sub[1])})] = new Vector128<double>().WithElement(0, {GenerateExpression(list[2])}).As<double, float>();";
								return;
							
							case PName("sr"):
								c += $"SR({GenerateExpression(sub[1])}, {GenerateExpression(sub[2])}, {GenerateExpression(sub[3])}, {GenerateExpression(sub[4])}, {GenerateExpression(sub[5])}, {GenerateExpression(list[2])});";
								return;
						}

					c += $"{GenerateExpression(list[1], lhs: true)} = {GenerateExpression(list[2])};";
				},
				(c, list) => {
					if(list[1] is PList sub)
						switch(sub[0]) {
							case PName("gpr32"):
								c += $"XR[(int) {GenerateExpression(sub[1])}] = (RuntimeValue<ulong>) (RuntimeValue<uint>) ({GenerateExpression(list[2])});";
								return;
							case PName("gpr-or-sp32"):
								c += $"if({GenerateExpression(sub[1])} == 31)";
								c++;
								c += $"SPR = (RuntimeValue<ulong>) (RuntimeValue<uint>) ({GenerateExpression(list[2])});";
								c--;
								c += "else";
								c++;
								c += $"XR[(int) {GenerateExpression(sub[1])}] = (RuntimeValue<ulong>) (RuntimeValue<uint>) ({GenerateExpression(list[2])});";
								c--;
								return;
							case PName("gpr64"):
								c += $"XR[(int) {GenerateExpression(sub[1])}] = {GenerateExpression(list[2])};";
								return;
							case PName("gpr-or-sp64"):
								c += $"if({GenerateExpression(sub[1])} == 31)";
								c++;
								c += $"SPR = {GenerateExpression(list[2])};";
								c--;
								c += "else";
								c++;
								c += $"XR[(int) {GenerateExpression(sub[1])}] = {GenerateExpression(list[2])};";
								c--;
								return;
							case PName("sr"):
								c += $"CallSR({GenerateExpression(sub[1])}, {GenerateExpression(sub[2])}, {GenerateExpression(sub[3])}, {GenerateExpression(sub[4])}, {GenerateExpression(sub[5])}, {GenerateExpression(list[2])});";
								return;
						}

					c += $"{GenerateExpression(list[1], lhs: true)} = {GenerateExpression(list[2])};";
				});
			
			Statement("let", 
				list => list.Last().Type.AsRuntime(list[2].Type.Runtime),
				(c, list) => {
					c += $"var {list[1]} = {GenerateExpression(list[2])};";
					list.Skip(3).ForEach(x => GenerateStatement(c, (PList) x));
				}, (c, list) => {
					if(list[2].Type.Runtime)
						c += $"var {list[1]} = ({GenerateExpression(list[2])}).Store();";
					else
						c += $"var {list[1]} = {GenerateExpression(list[2])};";
					list.Skip(3).ForEach(x => GenerateStatement(c, (PList) x));
				});

			Statement("mlet", 
				list => list.Last().Type.AsRuntime(list.AnyRuntime),
				(c, list) => {
					if(!(list[1] is PList dlist)) throw new NotSupportedException();
					Debug.Assert(dlist.Count % 2 == 0);
					for(var i = 0; i < dlist.Count; i += 2)
						c += $"var {dlist[i]} = {GenerateExpression(dlist[i + 1])};";
					list.Skip(2).ForEach(x => GenerateStatement(c, (PList) x));
				}, (c, list) => {
					if(!(list[1] is PList dlist)) throw new NotSupportedException();
					Debug.Assert(dlist.Count % 2 == 0);
					for(var i = 0; i < dlist.Count; i += 2)
						if(dlist[i + 1].Type.Runtime)
							c += $"var {dlist[i]} = ({GenerateExpression(dlist[i + 1])}).Store();";
						else
							c += $"var {dlist[i]} = {GenerateExpression(dlist[i + 1])};";
					list.Skip(2).ForEach(x => GenerateStatement(c, (PList) x));
				});
			
			Expression("cast", list => TypeFromName(list[2]).AsRuntime(list.AnyRuntime), 
				list => $"({GenerateType(list.Type)}) ({GenerateExpression(list[1])})");
			Expression("bitcast", list => TypeFromName(list[2]).AsRuntime(list.AnyRuntime), 
				list => $"Bitcast<{GenerateType(list[1].Type)}, {GenerateType(list.Type)}>({GenerateExpression(list[1])})", 
				list => $"({GenerateExpression(list[1])}).Bitcast<{GenerateType(list.Type.AsCompiletime())}>()");
			Expression("signext", list => TypeFromName(list[2]).AsRuntime(list.AnyRuntime), 
				list => $"SignExt<{GenerateType(list.Type)}>({GenerateExpression(list[1])}, {((EInt) list[1].Type).Width})", 
				list => $"SignExtRuntime<{GenerateType(list.Type.AsCompiletime())}>({GenerateExpression(list[1])}, {((EInt) list[1].Type).Width})");

			Expression(new[] { "==", "!=", ">", ">=", "<=", "<" },
				list => new EInt(false, 1).AsRuntime(list.AnyRuntime),
				list => $"(({GenerateExpression(list[1])}) {list[0]} ({GenerateExpression(list[2])})) ? 1U : 0U",
				list => $"({GenerateExpression(list[1])}) {list[0]} ({GenerateExpression(list[2])})");
		}
	}
}