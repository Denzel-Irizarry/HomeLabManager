namespace HomeLabManager.API.ExceptionsAPI
{
    public class BarcodeNotFoundException:Exception
    {
        public BarcodeNotFoundException():base ("No bardcode detected in the image.")
        {

        }
    }
}
