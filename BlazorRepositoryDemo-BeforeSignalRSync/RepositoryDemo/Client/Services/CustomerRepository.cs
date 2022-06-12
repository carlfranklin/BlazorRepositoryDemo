public class CustomerRepository : APIRepository<Customer>
{
    HttpClient http;
    
    // swap out the controller name
    //static string controllerName = "inmemorycustomers";
    //static string controllerName = "efcustomers";
    static string controllerName = "dappercustomers";

    public CustomerRepository(HttpClient _http)
       : base(_http, controllerName, "Id")
    {
        http = _http;
    }
}