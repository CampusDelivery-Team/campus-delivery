namespace CampusRunnerSystem.ViewModels;

public class LoginUserViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
}
