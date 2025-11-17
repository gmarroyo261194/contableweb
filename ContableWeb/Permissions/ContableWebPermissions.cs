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
    
    public static class Servicios
    { 
        public const string Default = GroupName + ".Servicios";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
    
    public static class TiposComprobantes
    {
        public const string Default = GroupName + ".TiposComprobantes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
    
    public static class Clientes
    {
        public const string Default = GroupName + ".Clientes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
}
