using System;
using System.Diagnostics;
using System.Linq;

namespace Generator {
	class ScalarMath : Builtin {
		static EType FirstType(PList list) => list[1].Type.AsRuntime(list.AnyRuntime);
		
		static EType LogicalType(EType a, EType b) {
			if(a is EInt || b is EInt) {
				if(!(a is EInt ai)) throw new NotSupportedException("Logical expression contains lhs that is non-int");
				if(!(b is EInt bi)) throw new NotSupportedException("Logical expression contains rhs that is non-int");
				return new EInt(
					ai.Signed == bi.Signed && ai.Signed,
					Math.Max(ai.Width, bi.Width)
				) { Runtime = ai.Runtime || bi.Runtime };
			}
			if(a is EFloat || b is EFloat) {
				if(!(a is EFloat af)) throw new NotSupportedException("Logical expression contains lhs that is non-float");
				if(!(b is EFloat bf)) throw new NotSupportedException("Logical expression contains rhs that is non-float");
				return new EFloat(Math.Max(af.Width, bf.Width)) { Runtime = af.Runtime || bf.Runtime };
			}
			throw new NotImplementedException("Logical expression has non-int/non-float type");
		}

		static EType LogicalType(PList list) => list.Skip(1).Select(x => x.Type).Aggregate(LogicalType);
		
		public override void Define() {
			Expression(
				new[] { "+", "-", "*", "/", "%" }, LogicalType,
				list => {
					Debug.Assert(list.Count == 3);
					if(list[1].Type is EInt(var sa, var ba) && list[2].Type is EInt(var sb, var bb)) {
						var stype = new EInt(sa && sb, Math.Max(ba, bb))
							{ Runtime = list[1].Type.Runtime || list[2].Type.Runtime };
						return
							$"(({GenerateType(stype)}) ({GenerateType(list[1].Type.AsRuntime(list.Type.Runtime))}) ({GenerateExpression(list[1])})) {list[0]} (({GenerateType(stype)}) ({GenerateType(list[2].Type.AsRuntime(list.Type.Runtime))}) ({GenerateExpression(list[2])}))";
					}

					if(list[1].Type is EFloat(var wa) && list[2].Type is EFloat(var wb)) {
						var stype = new EFloat(Math.Max(wa, wb))
							{ Runtime = list[1].Type.Runtime || list[2].Type.Runtime };
						return
							$"(({GenerateType(stype)}) ({GenerateType(list[1].Type.AsRuntime(list.Type.Runtime))}) ({GenerateExpression(list[1])})) {list[0]} (({GenerateType(stype)}) ({GenerateType(list[2].Type.AsRuntime(list.Type.Runtime))}) ({GenerateExpression(list[2])}))";
					}

					throw new NotImplementedException();
				});
			
			Expression(
				new[] { "|", "&", "^" }, FirstType,
				list => {
					var signed = true;
					var size = 0;
					foreach(var elem in list.Skip(1)) {
						if(!(elem.Type is EInt(var s, var ba)))
							throw new NotImplementedException();
						signed = signed && s;
						size = Math.Max(size, ba);
					}
					var stype = GenerateType(new EInt(signed, size).AsRuntime(list.AnyRuntime));
					return list.Skip(1).Select(x => $"(({stype}) ({GenerateExpression(x)}))").Aggregate((x1, x2) => $"({x1} {list[0]} {x2})");
				});
			
			Expression("~", FirstType, list => $"~({GenerateExpression(list[1])})");
			Expression("-!", FirstType, list => $"-({GenerateExpression(list[1])})");
			Expression("!", list => new EInt(false, 1).AsRuntime(list[1].Type.Runtime), 
				list => $"({GenerateExpression(list[1])}) != 0 ? 0U : 1U", list => $"!({GenerateExpression(list[1])})");
			
			Expression("<<", FirstType, 
				list => $"({GenerateExpression(list[1])}) << (int) ({GenerateExpression(list[2])})", 
				list => $"({GenerateExpression(list[1])}).ShiftLeft({GenerateExpression(list[2])})");
			
			Expression(">>", FirstType, 
				list => $"({GenerateExpression(list[1])}) >> (int) ({GenerateExpression(list[2])})", 
				list => $"({GenerateExpression(list[1])}).ShiftRight({GenerateExpression(list[2])})");
			
			Expression(">>>", FirstType,
				list => {
					if(!(list[1].Type is EInt(false, var bs))) throw new NotSupportedException();
					return
						$"(({GenerateExpression(list[1])}) << ({bs} - (int) ({GenerateExpression(list[2])}))) | (({GenerateExpression(list[1])}) >> (int) ({GenerateExpression(list[2])}))";
				}, list => {
					if(!(list[1].Type is EInt(false, var bs))) throw new NotSupportedException();
					return
						$"(({GenerateExpression(list[1])}).ShiftLeft((RuntimeValue<uint>) ({bs} - ({GenerateExpression(list[2])})))) | (({GenerateExpression(list[1])}).ShiftRight((RuntimeValue<uint>) ({GenerateExpression(list[2])})))";
				});

			Expression(":", list => new EInt(false,
					list.Skip(1).Select(y => y.Type is EInt(_, var width) ? width : throw new NotSupportedException())
						.Sum()).AsRuntime(list.AnyRuntime),
				list => {
					var offset = 0;
					return list.Skip(1).Reverse().Select(x => {
						if(!(x.Type is EInt(_, var width))) throw new NotSupportedException();
						var ret = $"((({GenerateType(list.Type)}) ({GenerateExpression(x)})) << {offset})";
						offset += width;
						return ret;
					}).Aggregate((a, x) =>
						$"({GenerateType(list.Type)}) ((({GenerateType(list.Type)}) {a}) | (({GenerateType(list.Type)}) {x}))");
				});

			Expression("replicate", list => new EInt(false,
					list[1].Type is EInt(_, var elemWidth) && list[2] is PInt(var count)
						? elemWidth * (int) count
						: throw new NotSupportedException()).AsRuntime(list[1].Type.Runtime),
				list => {
					if(!(list[1].Type is EInt(_, var width))) throw new NotSupportedException();
					if(!(list[2] is PInt(var count))) throw new NotSupportedException();
					return Enumerable.Range(0, (int) count)
						.Select(i => $"((({GenerateType(list.Type)}) ({GenerateExpression(list[1])})) << {i * width})")
						.Aggregate((a, x) =>
							$"({GenerateType(list.Type)}) ((({GenerateType(list.Type)}) {a}) | (({GenerateType(list.Type)}) {x}))");
				});

			Expression("sqrt", list => list[1].Type,
				list => $"({GenerateType(list.Type)}) Math.Sqrt((double) ({GenerateExpression(list[1])}))",
				list => $"({GenerateType(list.Type)}) (({GenerateType(new EFloat(64).AsRuntime(list[1].Type.Runtime))}) ({GenerateExpression(list[1])})).Sqrt()");
			
			Expression("bitwidth", _ => new EInt(true, 32),
				list => {
					switch(TypeFromName(list[1])) {
						case EInt(_, var iwidth): return iwidth.ToString();
						case EFloat(var fwidth): return fwidth.ToString();
						case EVector _: return "128";
						default: throw new NotSupportedException(list[1].Type.ToString());
					}
				});

			Expression("NaN?", list => new EInt(false, 1).AsRuntime(list[1].Type.Runtime),
				list => $"{(!(list[1].Type is EFloat(var fwidth)) ? throw new NotSupportedException() : fwidth == 32 ? "float" : "double")}.IsNaN({GenerateExpression(list[1])}) ? 1U : 0U",
				list => $"({GenerateExpression(list[1])}).IsNaN()");
		}
	}
}