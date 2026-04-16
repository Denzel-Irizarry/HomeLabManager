namespace HomeLabManager.WEBUI.Models
{
    public class UiPreferences
    {
        public string Theme { get; set; } = "Light";
        public string CardDensity { get; set; } = "Comfortable";
        public int TablePageSize { get; set; } = 25;
        public bool CompactHeaders { get; set; }
        public bool ShowDeleteConfirmation { get; set; } = true;
        public bool ClearRegisterImageAfterSave { get; set; } = true;
        public string DefaultLandingPage { get; set; } = "/dashboard";
    }
}
