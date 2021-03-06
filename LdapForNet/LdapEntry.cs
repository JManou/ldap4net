﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LdapForNet.Utils;

namespace LdapForNet
{
	public class LdapEntry
	{
		public string Dn { get; set; }

		[Obsolete]
		public Dictionary<string, List<string>> Attributes
		{
			get { return DirectoryAttributes.ToDictionary(_ => _.Name, _ => _.GetValues<string>().ToList()); }
			set
			{
				DirectoryAttributes = new SearchResultAttributeCollection();
				foreach (var attribute in value)
				{
					var directoryAttribute = new DirectoryAttribute
					{
						Name = attribute.Key
					};
					directoryAttribute.AddValues(attribute.Value);
					DirectoryAttributes.Add(directoryAttribute);
				}
			}
		}

		public SearchResultAttributeCollection DirectoryAttributes { get; set; }

		public DirectoryEntry ToDirectoryEntry()
		{
			return new DirectoryEntry {Dn = Dn, Attributes = DirectoryAttributes};
		}
	}

	public class DirectoryEntry
	{
		public string Dn { get; set; }
		public SearchResultAttributeCollection Attributes { get; set; }

		public LdapEntry ToLdapEntry()
		{
			return new LdapEntry
			{
				Dn = Dn,
				DirectoryAttributes = Attributes
			};
		}

		public DirectoryAttribute GetAttribute(string attribute) => Attributes.FirstOrDefault(x => string.Equals(x.Name, attribute, StringComparison.OrdinalIgnoreCase));

		private static Guid? GetGuid(byte[] bytes) => bytes != null && bytes.Length == 16 ? (Guid?) new Guid(bytes) : null;

		public string GetObjectSid() => LdapSidConverter.ParseFromBytes(GetBytes("objectSid"));

		public IEnumerable<string> GetObjectClass() => GetStrings(LdapAttributes.ObjectClass);

		public IEnumerable<string> GetSubRefs() => GetStrings(LdapAttributes.SubRefs);

		public Guid? GetObjectGuid()
		{
			var objectGuid = GetAttribute(LdapAttributes.ObjectGuid);
			return objectGuid != null ? GetGuid(objectGuid.GetValue<byte[]>()) : null;
		}

		public DateTime? GetWhenChanged()
		{
			var whenChanged = GetString(LdapAttributes.WhenChanged);
			if (whenChanged != null)
			{
				var date = DateTime.ParseExact(whenChanged, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeLocal);
				return DateTime.SpecifyKind(date, DateTimeKind.Utc);
			}

			return null;
		}

		public DateTime? GetModifyTimestamp()
		{
			var modifyTimestamp = GetString(LdapAttributes.ModifyTimestamp);
			if (modifyTimestamp != null)
			{
				var date = DateTime.ParseExact(modifyTimestamp, "yyyyMMddHHmmss'Z'", CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeLocal);
				return DateTime.SpecifyKind(date, DateTimeKind.Utc);
			}

			return null;
		}

		public IEnumerable<string> GetMemberOf() => GetStrings(LdapAttributes.MemberOf);

		public UserAccountControl GetUserAccountControl()
		{
			var attribute = GetString(LdapAttributes.UserAccountControl);
			return attribute == null ? UserAccountControl.NONE : (UserAccountControl) int.Parse(attribute);
		}

		public int GetPrimaryGroupID()
		{
			var attribute = GetString(LdapAttributes.PrimaryGroupID);
			return attribute == null ? 0 : int.Parse(attribute);
		}

		public int GetUserPrimaryID()
		{
			var objectSid = GetAttribute(LdapAttributes.ObjectSid)?.GetValue<byte[]>();
			if (objectSid != null)
				return BitConverter.ToInt32(objectSid, objectSid.Length - 4); //last 4 bytes are primary group id

			return -1;
		}

		public string GetString(string attributeName) => GetAttribute(attributeName)?.GetValue<string>();

		public byte[] GetBytes(string attributeName) => GetAttribute(attributeName)?.GetValue<byte[]>();

		public IEnumerable<string> GetStrings(string attributeName) => GetAttribute(attributeName)?.GetValues<string>() ?? Enumerable.Empty<string>();

		public IEnumerable<byte[]> GetByteArrays(string attributeName) => GetAttribute(attributeName)?.GetValues<byte[]>() ?? Enumerable.Empty<byte[]>();
	}

	public class LdapModifyEntry
	{
		public string Dn { get; set; }
		public List<LdapModifyAttribute> Attributes { get; set; }
	}

	public class LdapModifyAttribute
	{
		public string Type { get; set; }
		public List<string> Values { get; set; }

		public Native.Native.LdapModOperation LdapModOperation { get; set; } =
			Native.Native.LdapModOperation.LDAP_MOD_REPLACE;
	}

	public class DirectoryAttribute
	{
		private readonly List<object> _values = new List<object>();

		public string Name { get; set; }

		public T GetValue<T>()
			where T : class, IEnumerable
		{
			var items = GetValues<T>();
			var item = items.FirstOrDefault();
			return item == default(T) ? default : item;
		}

		public IEnumerable<T> GetValues<T>() where T : class, IEnumerable
		{
			if (!_values.Any()) return Enumerable.Empty<T>();

			var type = typeof(T);
			var valuesType = _values.First().GetType();
			if (type == valuesType) return _values.Select(_ => _ as T);

			if (type == typeof(byte[]) && valuesType == typeof(sbyte[])) return _values.Select(_ => _ as T);

			if (type == typeof(string))
				return _values.Select(_ => Encoder.Instance.GetString((byte[]) _))
					.Select(_ => _ as T);

			if (type == typeof(byte[]))
				return _values.Select(_ => Encoder.Instance.GetBytes((string) _))
					.Select(_ => _ as T);

			throw new NotSupportedException(
				$"Not supported type. You could specify 'string' or 'byte[]' of generic methods. Your type is {type.Name}");
		}

		internal List<object> GetRawValues()
		{
			return _values;
		}

		public void Add<T>(T value) where T : class, IEnumerable
		{
			ThrowIfWrongType<T>();
			_values.Add(value);
		}

		public void AddValues<T>(IEnumerable<T> values) where T : class, IEnumerable
		{
			ThrowIfWrongType<T>();
			_values.AddRange(values);
		}

		private void ThrowIfWrongType<T>() where T : class, IEnumerable
		{
			var type = typeof(T);
			if (type != typeof(string) && type != typeof(byte[]) && type != typeof(sbyte[]))
				throw new NotSupportedException(
					$"Not supported type. You could specify 'string' or 'byte[]' of generic methods. Your type is {type.Name}");

			if (_values.Any() && _values.First().GetType() != type)
				throw new NotSupportedException($"Not supported type. Type of values is {_values.First().GetType()}");
		}
	}

	public class DirectoryModificationAttribute : DirectoryAttribute
	{
		public Native.Native.LdapModOperation LdapModOperation { get; set; } =
			Native.Native.LdapModOperation.LDAP_MOD_REPLACE;
	}

	public abstract class DirectoryAttributeCollectionBase<T> : List<T>
		where T : DirectoryAttribute
	{
		public IEnumerable<string> AttributeNames =>
			this.Select(x => x.Name);

		public bool Contains(string attribute)
		{
			return this.Any(x => string.Equals(x.Name, attribute, StringComparison.OrdinalIgnoreCase));
		}

		public DirectoryAttribute this[string attribute]
		{
			get
			{
				var item = this.FirstOrDefault(
					x => string.Equals(x.Name, attribute, StringComparison.OrdinalIgnoreCase));

				if (item == null) throw new KeyNotFoundException();

				return item;
			}
		}

		public bool TryGetValue(string attribute, out DirectoryAttribute item)
		{
			item = this.FirstOrDefault(x => string.Equals(x.Name, attribute, StringComparison.OrdinalIgnoreCase));

			if (item == null) return false;

			return true;
		}

		public bool Remove(string attribute)
		{
			var found = false;

			for (var i = 0; i < Count; i++)
				if (string.Equals(this[i].Name, attribute, StringComparison.OrdinalIgnoreCase))
				{
					RemoveAt(i);
					--i;

					found = true;
				}

			return found;
		}
	}

	public class SearchResultAttributeCollection : KeyedCollection<string, DirectoryAttribute>
	{
		internal SearchResultAttributeCollection()
			: base(StringComparer.OrdinalIgnoreCase)
		{
		}

		public ICollection<string> AttributeNames => Dictionary.Keys;

		protected override string GetKeyForItem(DirectoryAttribute item)
		{
			return item.Name;
		}
	}

	public class ModifyAttributeCollection : DirectoryAttributeCollectionBase<DirectoryModificationAttribute>
	{
		internal ModifyAttributeCollection()
		{
		}
	}
}