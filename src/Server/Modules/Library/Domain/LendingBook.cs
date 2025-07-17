namespace OnlineShopModular.Server.Modules.Library.Domain
{
    public class LendingBook
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public Guid PersonId { get; set; }
        public DateTime LendOn { get; set; }
        public DateTime? ReturnOn { get; set; }
    }
}
