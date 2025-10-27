namespace ContableWeb.Permissions;

public static class ContableWebPermissions
{
    public const string GroupName = "ContableWeb";


    public static class Books
    {
        public const string Default = GroupName + ".Books";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Rubros
    {
        public const string Default = GroupName + ".Rubros";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
}
