using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RepEngine.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        // Index page loads without server-side data
        // Score fetching is handled via JavaScript API calls
    }
}
