namespace Fusion.Addons.Inspector.Editor
{
	using System.Reflection;
	using Mono.Cecil;
	using Mono.Cecil.Cil;
	using Mono.Collections.Generic;
	using UnityEngine;

	internal static class ScriptUtility
	{
		public static int GetLineNumber(PropertyInfo propertyInfo)
		{
			MethodInfo accessor = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
			if (accessor == null)
				return -1;

			return GetLineNumber(accessor);
		}

		public static int GetLineNumber(MethodInfo methodInfo)
		{
			AssemblyDefinition assemblyDefinition = LoadAssembly(methodInfo.DeclaringType.Assembly);
			if (assemblyDefinition == null)
				return -1;

			using (assemblyDefinition)
			{
				MethodDefinition methodDefinition = FindMethod(assemblyDefinition, methodInfo);
				if (methodDefinition == null || methodDefinition.HasBody == false || methodDefinition.DebugInformation == null)
					return -1;

				Collection<SequencePoint> sequencePoints = methodDefinition.DebugInformation.SequencePoints;
				if (sequencePoints == null)
					return -1;

				foreach (SequencePoint sequencePoint in sequencePoints)
				{
					if (sequencePoint.StartLine > 0)
						return sequencePoint.StartLine;
				}

				return -1;
			}
		}

		private static MethodDefinition FindMethod(AssemblyDefinition assemblyDefinition, MethodInfo methodInfo)
		{
			string typeFullName = methodInfo.DeclaringType.FullName.Replace('+', '/');

			ParameterInfo[] parameters = methodInfo.GetParameters();

			foreach (ModuleDefinition module in assemblyDefinition.Modules)
			{
				foreach (TypeDefinition type in module.GetTypes())
				{
					if (type.FullName != typeFullName)
						continue;

					MethodDefinition arityMatch = default;

					foreach (MethodDefinition method in type.Methods)
					{
						if (method.Name != methodInfo.Name || method.Parameters.Count != parameters.Length)
							continue;

						if (HasMatchingParameters(method, parameters) == true)
							return method;

						if (arityMatch == default)
						{
							arityMatch = method;
						}
					}

					return arityMatch;
				}
			}

			return default;
		}

		private static bool HasMatchingParameters(MethodDefinition method, ParameterInfo[] parameters)
		{
			for (int i = 0; i < parameters.Length; ++i)
			{
				if (method.Parameters[i].ParameterType.Name != parameters[i].ParameterType.Name)
					return false;
			}

			return true;
		}

		private static AssemblyDefinition LoadAssembly(Assembly assembly)
		{
			try
			{
				ReaderParameters readerParameters = new ReaderParameters
				{
					ReadSymbols = true,
					ReadingMode = ReadingMode.Immediate
				};

#if UNITY_6000_6_OR_NEWER
				return AssemblyDefinition.ReadAssembly(assembly.GetLoadedAssemblyPath(), readerParameters);
#else
				return AssemblyDefinition.ReadAssembly(assembly.Location, readerParameters);
#endif
			}
			catch
			{
				return default;
			}
		}
	}
}
