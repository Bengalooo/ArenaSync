namespace ArenaSync.Data
{
    // Role systemu (RBAC)
    public static class Role
    {
        public const string Admin = "Admin";       // turnieje, drużyny, wyniki, konta (odblokowanie, reset hasła), role
        public const string Kapitan = "Kapitan";   // zarządza swoją drużyną i kadrą
        public const string Zawodnik = "Zawodnik"; // członek drużyny: przegląda wyniki i terminarz

        public static readonly string[] Wszystkie = { Admin, Kapitan, Zawodnik };

        public static string Nazwa(string rola) => rola switch
        {
            Admin => "Administrator",
            Kapitan => "Kapitan",
            Zawodnik => "Zawodnik",
            _ => rola
        };
    }
}
