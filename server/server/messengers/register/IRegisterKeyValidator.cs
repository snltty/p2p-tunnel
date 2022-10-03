namespace server.messengers.register
{
    public interface IRegisterKeyValidator
    {
        public bool Validate(string key);
    }
}
