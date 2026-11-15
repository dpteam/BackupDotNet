namespace RVTools.LoginSSPI.Security;

public enum Type : uint
{
	RESOURCETYPE_ANY = 0u,
	RESOURCETYPE_DISK = 1u,
	RESOURCETYPE_PRINT = 2u,
	RESOURCETYPE_RESERVED = 8u,
	RESOURCETYPE_UNKNOWN = uint.MaxValue
}
