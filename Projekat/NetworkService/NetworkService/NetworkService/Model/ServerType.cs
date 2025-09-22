namespace NetworkService.Model
{
    public class ServerType
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }

        public ServerType(string name, string imagePath)
        {
            Name = name;
            ImagePath = imagePath;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}