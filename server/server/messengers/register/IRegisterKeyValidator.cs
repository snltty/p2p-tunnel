namespace server.messengers.singnin
{
    public interface ISignInValidator
    {
        public bool Validate(string key);
    }
}
