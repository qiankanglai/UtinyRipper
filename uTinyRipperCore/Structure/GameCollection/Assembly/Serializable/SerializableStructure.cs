using System;
using System.Collections.Generic;
using uTinyRipper.Classes;
using uTinyRipper.Converters;
using uTinyRipper.SerializedFiles;
using uTinyRipper.YAML;

using Object = uTinyRipper.Classes.Object;

namespace uTinyRipper.Game.Assembly
{
	public sealed class SerializableStructure : IAsset, IDependent
	{
		internal SerializableStructure(SerializableType type, int depth)
		{
			Depth = depth;
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Fields = new SerializableField[type.FieldCount];
		}

		public void Read(AssetReader reader)
		{
			for (int i = 0; i < Fields.Length; i++)
			{
				SerializableType.Field etalon = Type.GetField(i);
				if (IsAvailable(etalon))
				{
					Fields[i].Read(reader, Depth, etalon, null, 0);
				}
			}
		}

		public void Read(AssetReader reader, TypeTreeNode[] TreeNodes, int TreeNodeIdx)
		{
			for (int i = 0; i < Fields.Length; i++)
			{
				SerializableType.Field etalon = Type.GetField(i);
				if (IsAvailable(etalon))
				{
					int foundIdx = -1;
					for (int j = TreeNodeIdx; j < TreeNodes.Length; j++)
					{
						if (TreeNodes[j].Depth == Depth + 1 && TreeNodes[j].Name == etalon.Name)
						{
							foundIdx = j;
							break;
						}
					}
					if (foundIdx < 0)
						continue;
					while (TreeNodeIdx < foundIdx)
					{
						if (TreeNodes[TreeNodeIdx].Depth == Depth + 1)
							reader.Skip(TreeNodes[TreeNodeIdx].ByteSize);
						//reader.ReadInt
						TreeNodeIdx++;
					}
					Fields[i].Read(reader, Depth, etalon, TreeNodes, TreeNodeIdx);
					// move through current TreeNode
					TreeNodeIdx++;
					while (TreeNodeIdx < TreeNodes.Length && TreeNodes[TreeNodeIdx].Depth > Depth + 1)
						TreeNodeIdx++;
				}
			}
		}

		public void Write(AssetWriter writer)
		{
			for (int i = 0; i < Fields.Length; i++)
			{
				SerializableType.Field etalon = Type.GetField(i);
				if (IsAvailable(etalon))
				{
					Fields[i].Write(writer, etalon);
				}
			}
		}

		public YAMLNode ExportYAML(IExportContainer container)
		{
			YAMLMappingNode node = new YAMLMappingNode();
			for (int i = 0; i < Fields.Length; i++)
			{
				SerializableType.Field etalon = Type.GetField(i);
				if (IsAvailable(etalon))
				{
					var v = Fields[i].ExportYAML(container, etalon);
					if (v != null)
						node.Add(etalon.Name, v);
				}
			}
			return node;
		}

		public IEnumerable<PPtr<Object>> FetchDependencies(DependencyContext context)
		{
			for (int i = 0; i < Fields.Length; i++)
			{
				SerializableType.Field etalon = Type.GetField(i);
				if (IsAvailable(etalon))
				{
					foreach (PPtr<Object> asset in Fields[i].FetchDependencies(context, etalon))
					{
						yield return asset;
					}
				}
			}
		}

		public override string ToString()
		{
			if (Type.Namespace.Length == 0)
			{
				return $"{Type.Name}";
			}
			else
			{
				return $"{Type.Namespace}.{Type.Name}";
			}
		}

		private bool IsAvailable(in SerializableType.Field field)
		{
			if (Depth < MaxDepthLevel)
			{
				return true;
			}
			if (field.IsArray)
			{
				return false;
			}
			if (field.Type.Type	== PrimitiveType.Complex)
			{
				if (SerializableType.IsEngineStruct(field.Type.Namespace, field.Type.Name))
				{
					return true;
				}
				return false;
			}
			return true;
		}

		public int Depth { get; }
		public SerializableType Type { get; }
		public SerializableField[] Fields { get; }

		public const int MaxDepthLevel = 8;
	}
}
