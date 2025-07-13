using OnlineShopModular.Server.Api.Models.Attachments;

namespace OnlineShopModular.Server.Api.Data.Configurations.Attachments;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(attachment => new { attachment.Id, attachment.Kind });
    }
}
