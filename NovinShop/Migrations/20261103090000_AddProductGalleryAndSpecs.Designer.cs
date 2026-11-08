using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NovinShop.Data;

#nullable disable

namespace NovinShop.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261103090000_AddProductGalleryAndSpecs")]
    partial class AddProductGalleryAndSpecs
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
#pragma warning restore 612, 618
        }
    }
}
