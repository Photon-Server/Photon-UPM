namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Text;

	internal static class ReflectionUtility
	{
		private static readonly Dictionary<Type, string> _userFriendlyTypeNames = new Dictionary<Type, string>
		{
			{ typeof(void),    "void"    },
			{ typeof(bool),    "bool"    },
			{ typeof(byte),    "byte"    },
			{ typeof(sbyte),   "sbyte"   },
			{ typeof(short),   "short"   },
			{ typeof(ushort),  "ushort"  },
			{ typeof(int),     "int"     },
			{ typeof(uint),    "uint"    },
			{ typeof(long),    "long"    },
			{ typeof(ulong),   "ulong"   },
			{ typeof(float),   "float"   },
			{ typeof(double),  "double"  },
			{ typeof(decimal), "decimal" },
			{ typeof(char),    "char"    },
			{ typeof(string),  "string"  },
			{ typeof(object),  "object"  },
			{ typeof(IntPtr),  "nint"    },
			{ typeof(UIntPtr), "nuint"   },
		};

		public static PropertyInfo[] GetProperties(Type type)
		{
			List<PropertyInfo> properties = new List<PropertyInfo>();

			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				if (property.GetCustomAttribute<NetworkedAttribute>() != null)
				{
					properties.Add(property);
				}
			}

			return properties.ToArray();
		}

		public static MethodInfo[] GetRPCs(Type type)
		{
			List<MethodInfo> rpcs = new List<MethodInfo>();

			foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				if (method.GetCustomAttribute<RpcAttribute>() != null)
				{
					rpcs.Add(method);
				}
			}

			return rpcs.ToArray();
		}

		public static bool HasMethodOverride(Type type, string methodName)
		{
			MethodInfo method = type.GetMethod(methodName);
			return method != null && method.DeclaringType == type;
		}

		public static bool HasInterfaceImplementation(Type type, Type interfaceType)
		{
			foreach (Type implementedInterface in type.GetInterfaces())
			{
				if (implementedInterface == interfaceType)
					return true;
			}

			return false;
		}

		public static string GetPropertyDeclaration(PropertyInfo property)
		{
			StringBuilder sb = new StringBuilder();

			MethodInfo getter = property.GetGetMethod(true);
			MethodInfo setter = property.GetSetMethod(true);

			MethodInfo accessor = getter ?? setter;

			if (accessor == null)
				return property.Name;

			     if (accessor.IsPublic)   { sb.Append("public ");    }
			else if (accessor.IsPrivate)  { sb.Append("private ");   }
			else if (accessor.IsFamily)   { sb.Append("protected "); }
			else if (accessor.IsAssembly) { sb.Append("internal ");  }

			if (accessor.IsStatic) sb.Append("static ");

			if (accessor.IsAbstract) sb.Append("abstract ");
			else if (accessor.IsVirtual && accessor.IsFinal == false) sb.Append("virtual ");

			string typeName = GetUserFriendlyTypeName(property.PropertyType);
			sb.Append(typeName + " ");

			ParameterInfo[] indexParameters = property.GetIndexParameters();
			if (indexParameters.Length > 0)
			{
				sb.Append("this[");

				for (int i = 0; i < indexParameters.Length; ++i)
				{
					ParameterInfo parameter = indexParameters[i];

					string parameterType = GetUserFriendlyTypeName(parameter.ParameterType);

					sb.Append($"{parameterType} {parameter.Name}");

					if (i < indexParameters.Length - 1)
					{
						sb.Append(", ");
					}
				}

				sb.Append("]");
			}
			else
			{
				sb.Append(property.Name);
			}

			sb.Append(" { ");

			if (getter != null)
			{
				AppendAccessorVisibility(sb, getter, accessor);
				sb.Append("get; ");
			}

			if (setter != null)
			{
				AppendAccessorVisibility(sb, setter, accessor);
				sb.Append("set; ");
			}

			sb.Append("}");

			return sb.ToString();

			static void AppendAccessorVisibility(StringBuilder sb, MethodInfo accessor, MethodInfo propertyAccessor)
			{
				if (accessor == propertyAccessor)
					return;

				if (accessor.IsPrivate) sb.Append("private ");
				else if (accessor.IsFamily) sb.Append("protected ");
				else if (accessor.IsAssembly) sb.Append("internal ");
			}
		}

		public static string GetMethodDeclaration(MethodInfo method)
		{
			StringBuilder sb = new StringBuilder();

			     if (method.IsPublic   == true) { sb.Append("public ");    }
			else if (method.IsPrivate  == true) { sb.Append("private ");   }
			else if (method.IsFamily   == true) { sb.Append("protected "); }
			else if (method.IsAssembly == true) { sb.Append("internal ");  }

			if (method.IsStatic   == true)                       { sb.Append("static ");   }
			if (method.IsAbstract == true)                       { sb.Append("abstract "); }
			if (method.IsVirtual  == true && !method.IsAbstract) { sb.Append("virtual ");  }

			string returnType = GetUserFriendlyTypeName(method.ReturnType);

			sb.Append(returnType + " ");
			sb.Append(method.Name);
			sb.Append("(");

			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; ++i)
			{
				ParameterInfo parameterInfo = parameters[i];

				sb.Append($"{TypeUtility.GetTypeName(parameterInfo.ParameterType)} {parameterInfo.Name}");

				if (i < parameters.Length - 1)
				{
					sb.Append(", ");
				}
			}

			sb.Append(");");

			return sb.ToString();
		}

		private static string GetUserFriendlyTypeName(Type type)
		{
			if (_userFriendlyTypeNames.TryGetValue(type, out string userFriendlyTypeName))
				return userFriendlyTypeName;

			return TypeUtility.GetTypeName(type);
		}
	}
}
