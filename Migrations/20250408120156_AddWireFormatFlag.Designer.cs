﻿// <auto-generated />
using DNS_proxy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DNS_proxy.Migrations
{
    [DbContext(typeof(DnsRulesContext))]
    [Migration("20250408120156_AddWireFormatFlag")]
    partial class AddWireFormatFlag
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

            modelBuilder.Entity("DNS_proxy.Core.Models.DnsRule", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DomainPattern")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RewriteIp")
                        .HasColumnType("TEXT");

                    b.Property<string>("SourceIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("DnsRules");
                });

            modelBuilder.Entity("DNS_proxy.Core.Models.DnsServerEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDoh")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseWireFormat")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DnsServers");
                });
#pragma warning restore 612, 618
        }
    }
}
