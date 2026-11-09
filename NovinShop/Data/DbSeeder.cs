using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var roleName in new[] { AppRoles.Admin, AppRoles.Editor, AppRoles.Accountant })
        {
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole(roleName));
        }

        const string email = "admin@novinshop.local";
        if (!await users.Users.AnyAsync())
        {
            var admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(admin, "Admin@12345");
            if (result.Succeeded)
                await users.AddToRoleAsync(admin, AppRoles.Admin);
        }

        // کارمندان نمونه (ویرایشگر کاتالوگ و حسابدار) در صورت عدم وجود
        if (await users.FindByEmailAsync("editor@novinshop.local") is null)
        {
            var ed = new IdentityUser { UserName = "editor@novinshop.local", Email = "editor@novinshop.local", EmailConfirmed = true };
            if ((await users.CreateAsync(ed, "Staff@12345")).Succeeded)
                await users.AddToRoleAsync(ed, AppRoles.Editor);
        }
        if (await users.FindByEmailAsync("accountant@novinshop.local") is null)
        {
            var acc = new IdentityUser { UserName = "accountant@novinshop.local", Email = "accountant@novinshop.local", EmailConfirmed = true };
            if ((await users.CreateAsync(acc, "Staff@12345")).Succeeded)
                await users.AddToRoleAsync(acc, AppRoles.Accountant);
        }

        await SeedGatewaysAsync(db);
        await SeedCatalogAsync(db);
        await SeedContentAsync(db);
        await SeedDemoShowcaseAsync(db);
        await BackfillIconsAsync(db);
        await BackfillUnsplashImagesAsync(db);
    }

    // اختصاص تصاویر واقعی و باکیفیت Unsplash به محصولات، بنرها و مقالات وبلاگ (در صورت خالی بودن تصویر)
    private static async Task BackfillUnsplashImagesAsync(AppDbContext db)
    {
        static string U(string id, int w = 640) => $"https://images.unsplash.com/photo-{id}?w={w}&q=80&auto=format&fit=crop";

        var allProducts = await db.Products.Include(p => p.Category).ToListAsync();
        var changed = false;

        foreach (var p in allProducts)
        {
            if (!string.IsNullOrEmpty(p.ImagePath)) continue;
            var n = p.Name ?? "";
            var c = p.Category?.Slug ?? "";

            string mainId = c switch
            {
                "digital" => "1511707171634-5f897ff02aa9",
                "fashion" => "1542291026-7eec264c27ff",
                "home" => "1517668808822-9ebb02f2a0e6",
                "books" => "1544947950-fa07a98d237f",
                "beauty" => "1620916566398-39f1143ab7be",
                _ => "1583454110551-21f2fa2afe61"
            };
            string[] extraIds = c switch
            {
                "digital" => new[] { "1498049794561-7780e7231661", "1468495244123-6c6c332eeece" },
                "fashion" => new[] { "1441986300917-64674bd600d8", "1483985988355-763728e1935b" },
                "home" => new[] { "1556911220-bff31c812dba", "1556910103-1c02745aae4d" },
                "books" => new[] { "1512820790803-83ca734da794", "1495446815901-a7297e633e8d" },
                "beauty" => new[] { "1608248543803-ba4f8c70ae0b", "1556228578-8c89e6adf883" },
                _ => new[] { "1517836357463-d25dfeac3438", "1517963879433-6ad2b056d712" }
            };

            if (n.Contains("هدفون")) { mainId = "1505740420928-5e560c06d30e"; extraIds = new[] { "1618366712010-f4ae9c647dcb", "1484704849700-f032a568e944" }; }
            else if (n.Contains("ساعت هوشمند")) { mainId = "1546868871-7041f2a55e12"; extraIds = new[] { "1579586337278-3befd40fd17a", "1508685096489-7aacd43bd3b1" }; }
            else if (n.Contains("گوشی") || n.Contains("گلکسی")) { mainId = "1610945415295-d9bbf067e59c"; extraIds = new[] { "1511707171634-5f897ff02aa9", "1592899677977-9c10ca588bbd" }; }
            else if (n.Contains("لپ‌تاپ")) { mainId = "1496181133206-80ce9b88a853"; extraIds = new[] { "1517336714731-489689fd1ca8", "1525547719571-a2d4ac8945e2" }; }
            else if (n.Contains("پاوربانک")) { mainId = "1609091839311-d5365f9ff1c5"; extraIds = new[] { "1583863788434-e58a36330cf0", "1511707171634-5f897ff02aa9" }; }
            else if (n.Contains("کیبورد")) { mainId = "1587829741301-dc798b83add3"; extraIds = new[] { "1541140532154-b024d705b90a", "1593305841991-05c297ba4575" }; }
            else if (n.Contains("ماوس")) { mainId = "1527864550417-7fd91fc51a46"; extraIds = new[] { "1615663245857-ac93bb7c39e7", "1587829741301-dc798b83add3" }; }
            else if (n.Contains("کنسول") || n.Contains("PlayStation")) { mainId = "1606813907291-d86efa9b94db"; extraIds = new[] { "1607853202273-797f1c22a38e", "1542751371-adc38448a05e" }; }
            else if (n.Contains("تبلت")) { mainId = "1544244015-0df4b3ffc6b0"; extraIds = new[] { "1561154464-82e9adf32764", "1512941937669-90a1b58e7e9c" }; }
            else if (n.Contains("هودی")) { mainId = "1556821840-3a63f95609a7"; extraIds = new[] { "1620799140408-edc6dcb6d633", "1441986300917-64674bd600d8" }; }
            else if (n.Contains("کتانی") || n.Contains("کفش")) { mainId = "1542291026-7eec264c27ff"; extraIds = new[] { "1460353581641-37baddab0fa2", "1556821840-3a63f95609a7" }; }
            else if (n.Contains("کوله‌پشتی")) { mainId = "1553062407-98eeb64c6a62"; extraIds = new[] { "1622560480605-d83c853bc5c3", "1496181133206-80ce9b88a853" }; }
            else if (n.Contains("عینک")) { mainId = "1511499767150-a48a237f0083"; extraIds = new[] { "1572635196237-14b3f281503f", "1483985988355-763728e1935b" }; }
            else if (n.Contains("پیراهن")) { mainId = "1596755094514-f87e34085b2c"; extraIds = new[] { "1602810318383-e386cc2a3ccf", "1441986300917-64674bd600d8" }; }
            else if (n.Contains("کاپشن")) { mainId = "1551028719-00167b16eac5"; extraIds = new[] { "1591047139829-d91aecb6caea", "1483985988355-763728e1935b" }; }
            else if (n.Contains("کیف پول")) { mainId = "1627123424574-724758594e93"; extraIds = new[] { "1606503825008-909a67e63c3d", "1524592094714-0f0654e20314" }; }
            else if (n.Contains("ساعت مچی")) { mainId = "1524592094714-0f0654e20314"; extraIds = new[] { "1522312346375-d1a52e2b99b3", "1523275335684-37898b6baf30" }; }
            else if (n.Contains("قهوه‌ساز")) { mainId = "1517668808822-9ebb02f2a0e6"; extraIds = new[] { "1495474472287-4d71bcdd2085", "1556911220-bff31c812dba" }; }
            else if (n.Contains("سرخ‌کن")) { mainId = "1626509653291-18d9a934b9db"; extraIds = new[] { "1648134859175-78b41b4db186", "1556910103-1c02745aae4d" }; }
            else if (n.Contains("جاروبرقی")) { mainId = "1589894404892-7310b92ea7a2"; extraIds = new[] { "1603618090561-412154b4bd1b", "1556911220-bff31c812dba" }; }
            else if (n.Contains("چراغ مطالعه")) { mainId = "1507473885765-e6ed057f782c"; extraIds = new[] { "1513506003901-1e6a229e2d15", "1484480974693-6ca0a78fb36b" }; }
            else if (n.Contains("قابلمه")) { mainId = "1556909114-f6e7ad7d3136"; extraIds = new[] { "1556910103-1c02745aae4d", "1556911220-bff31c812dba" }; }
            else if (n.Contains("چای‌ساز") || n.Contains("کتری")) { mainId = "1594213114663-d94db9b17125"; extraIds = new[] { "1563822249366-3efb23b8e0c9", "1495474472287-4d71bcdd2085" }; }
            else if (n.Contains("مخلوط‌کن")) { mainId = "1570222094114-d054a817e56b"; extraIds = new[] { "1585237017125-24baf8d7406f", "1556910103-1c02745aae4d" }; }
            else if (n.Contains("یوگا") || n.Contains("زیرانداز")) { mainId = "1601925260368-ae2f83cf8b7f"; extraIds = new[] { "1592432678016-e910b452f9a2", "1517836357463-d25dfeac3438" }; }
            else if (n.Contains("قمقمه")) { mainId = "1602143407151-7111542de6e8"; extraIds = new[] { "1523362628745-0c100150b504", "1517836357463-d25dfeac3438" }; }
            else if (n.Contains("چادر")) { mainId = "1504280390367-361c6d9f38f4"; extraIds = new[] { "1478131143081-80f7f84ca84d", "1510312305653-8ed496efae75" }; }
            else if (n.Contains("دمبل")) { mainId = "1583454110551-21f2fa2afe61"; extraIds = new[] { "1517836357463-d25dfeac3438", "1517963879433-6ad2b056d712" }; }
            else if (n.Contains("کش ورزشی")) { mainId = "1598289431512-b97b0917affc"; extraIds = new[] { "1517963879433-6ad2b056d712", "1517836357463-d25dfeac3438" }; }
            else if (n.Contains("کیسه خواب")) { mainId = "1510312305653-8ed496efae75"; extraIds = new[] { "1487730116645-74489c95b41b", "1504280390367-361c6d9f38f4" }; }
            else if (n.Contains("سرم")) { mainId = "1620916566398-39f1143ab7be"; extraIds = new[] { "1608248543803-ba4f8c70ae0b", "1556228578-8c89e6adf883" }; }
            else if (n.Contains("مسواک")) { mainId = "1607613009820-a29f7bb81c04"; extraIds = new[] { "1556228578-8c89e6adf883", "1620916566398-39f1143ab7be" }; }
            else if (n.Contains("سشوار")) { mainId = "1522338140262-f46f5913618a"; extraIds = new[] { "1580618672591-eb180b1a973f", "1556228578-8c89e6adf883" }; }
            else if (n.Contains("ضدآفتاب") || n.Contains("کرم")) { mainId = "1556228578-8c89e6adf883"; extraIds = new[] { "1571781926291-c477ebfd024b", "1608248543803-ba4f8c70ae0b" }; }
            else if (n.Contains("اصلاح")) { mainId = "1621607512214-68297480165e"; extraIds = new[] { "1585751119414-ef2636f8aede", "1522338140262-f46f5913618a" }; }
            else if (n.Contains("ادوپرفیوم") || n.Contains("عطر")) { mainId = "1541643600914-78b084683601"; extraIds = new[] { "1523293182086-7651a899d37f", "1608248543803-ba4f8c70ae0b" }; }
            else if (n.Contains("بولت ژورنال") || n.Contains("دفتر")) { mainId = "1531346878377-a5be20888e57"; extraIds = new[] { "1517842645767-c639042777db", "1484480974693-6ca0a78fb36b" }; }
            else if (n.Contains("عادت")) { mainId = "1544947950-fa07a98d237f"; }
            else if (n.Contains("داده محور")) { mainId = "1532012197267-da84d127e765"; }
            else if (n.Contains("شفاف اندیشیدن")) { mainId = "1512820790803-83ca734da794"; }
            else if (n.Contains("کار عمیق")) { mainId = "1495446815901-a7297e633e8d"; }
            else if (n.Contains("روانشناسی پول")) { mainId = "1589829085413-56de8ae18c73"; }
            else if (n.Contains("کد تمیز") || n.Contains("کدنویسی")) { mainId = "1543002588-bfa74002ed7e"; }

            p.ImagePath = U(mainId, 640);
            if (string.IsNullOrEmpty(p.GalleryJson))
            {
                p.GalleryJson = System.Text.Json.JsonSerializer.Serialize(extraIds.Select(x => U(x, 720)).ToList());
            }
            changed = true;
        }

        var banners = await db.Banners.ToListAsync();
        foreach (var b in banners)
        {
            if (!string.IsNullOrEmpty(b.ImagePath)) continue;
            var t = b.Title ?? "";
            b.ImagePath = t.Contains("خانه") ? U("1556911220-bff31c812dba", 1400)
                        : t.Contains("پوشاک") ? U("1441986300917-64674bd600d8", 1400)
                        : U("1498049794561-7780e7231661", 1400);
            changed = true;
        }

        var posts = await db.BlogPosts.ToListAsync();
        foreach (var post in posts)
        {
            if (!string.IsNullOrEmpty(post.ImagePath)) continue;
            var s = post.Slug ?? "";
            post.ImagePath = s.Contains("smartphone") ? U("1512941937669-90a1b58e7e9c", 800)
                           : s.Contains("headphone") ? U("1484704849700-f032a568e944", 800)
                           : s.Contains("planning") ? U("1484480974693-6ca0a78fb36b", 800)
                           : s.Contains("gaming") ? U("1542751371-adc38448a05e", 800)
                           : s.Contains("weekend") ? U("1495474472287-4d71bcdd2085", 800)
                           : U("1556910103-1c02745aae4d", 800);
            changed = true;
        }

        if (changed) await db.SaveChangesAsync();
    }

    // ماژول‌های پیش‌فرض پرداخت (زرین‌پال، کارت به کارت، شبیه‌ساز دمو)
    private static async Task SeedGatewaysAsync(AppDbContext db)
    {
        if (await db.PaymentGateways.AnyAsync()) return;

        db.PaymentGateways.AddRange(
            new PaymentGateway
            {
                Key = "zarinpal",
                Title = "پرداخت آنلاین زرین‌پال",
                Description = "پرداخت آنی با تمامی کارت‌های بانکی عضو شتاب",
                IsEnabled = true,
                IsSandbox = true,
                SortOrder = 1,
                MerchantId = "1344b5d4-0048-11e8-94db-005056a205be" // کد نمونه ۳۶ کاراکتری برای Sandbox زرین‌پال
            },
            new PaymentGateway
            {
                Key = "card_to_card",
                Title = "کارت به کارت (ثبت تصویر فیش)",
                Description = "واریز وجه به شماره کارت فروشگاه و ارسال تصویر رسید برای تأیید",
                IsEnabled = true,
                IsSandbox = false,
                SortOrder = 2,
                CardNumber = "6037-9975-5412-8890",
                CardHolder = "مدیریت فروشگاه نوین‌شاپ",
                BankName = "بانک ملی ایران",
                ShebaNumber = "IR450170000000123456789001"
            },
            new PaymentGateway
            {
                Key = "simulator",
                Title = "درگاه آزمایشی سریع (دمو)",
                Description = "تست سریع پرداخت موفق یا ناموفق بدون نیاز به کارت بانکی",
                IsEnabled = true,
                IsSandbox = true,
                SortOrder = 3
            }
        );
        await db.SaveChangesAsync();
    }

    // آیکن برای دسته‌بندی‌های نمونه‌ی قدیمی که هنوز آیکن ندارند
    private static async Task BackfillIconsAsync(AppDbContext db)
    {
        var map = new Dictionary<string, string>
        {
            ["digital"] = "📱", ["fashion"] = "👕", ["home"] = "🏠",
            ["books"] = "📚", ["beauty"] = "💄", ["sports"] = "⚽"
        };
        var keys = map.Keys.ToList();
        var list = await db.Categories.Where(c => (c.Icon == null || c.Icon == "") && keys.Contains(c.Slug)).ToListAsync();
        foreach (var c in list) c.Icon = map[c.Slug];
        if (list.Count > 0) await db.SaveChangesAsync();
    }

    private static async Task SeedContentAsync(AppDbContext db)
    {
        if (await db.ContentPages.AnyAsync()) return;
        var pages = new[]
        {
            ("درباره ما", "# درباره‌ی ما\n\nما یک فروشگاه اینترنتی نمونه هستیم که با **ASP.NET Core** ساخته شده است.\n\n## چرا ما؟\n\n- کالای اصل و سالم\n- ارسال سریع به سراسر کشور\n- بازگشت کالا تا ۷ روز"),
            ("تماس با ما", "# تماس با ما\n\nتلفن پشتیبانی: ۰۲۱-۱۲۳۴۵۶۷۸\n\nساعات پاسخگویی: همه‌ی روزها از ۹ صبح تا ۹ شب"),
            ("قوانین و مقررات", "# قوانین و مقررات\n\nاین متن نمونه است. قوانین واقعی فروشگاه خود را از پنل مدیریت در همین صفحه بنویسید."),
            ("شرایط بازگشت کالا", "# شرایط بازگشت کالا\n\n- کالا باید دست‌نخورده و در بسته‌بندی اصلی باشد\n- مهلت بازگشت ۷ روز پس از تحویل است"),
        };
        db.ContentPages.AddRange(pages.Select((p, i) => new ContentPage
        {
            Title = p.Item1, Slug = SlugHelper.Generate(p.Item1), Content = p.Item2,
            IsActive = true, ShowInFooter = true, SortOrder = i + 1,
            SeoTitle = $"{p.Item1} | نوین‌شاپ", SeoDescription = p.Item1 + " فروشگاه نوین‌شاپ"
        }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var cats = new[]
        {
            ("دیجیتال", "digital", "📱"),
            ("پوشاک", "fashion", "👕"),
            ("خانه و آشپزخانه", "home", "🏠"),
            ("کتاب", "books", "📚"),
            ("زیبایی و سلامت", "beauty", "💄"),
            ("ورزش و سفر", "sports", "⚽")
        }
        .Select((c, i) => new Category
        {
            Name = c.Item1, Slug = c.Item2, Icon = c.Item3, IsActive = true, SortOrder = i + 1,
            SeoTitle = $"خرید {c.Item1} | نوین‌شاپ",
            SeoDescription = $"بهترین محصولات {c.Item1} با ضمانت اصالت و ارسال سریع"
        }).ToList();
        db.Categories.AddRange(cats);
        await db.SaveChangesAsync();
    }

    // تکمیل کاتالوگ دمو (دسته‌های جدید، ۲۴+ محصول متنوع، بنرهای اسلایدر و سفارش‌های نمونه برای بخش پرفروش‌ها)
    private static async Task SeedDemoShowcaseAsync(AppDbContext db)
    {
        // ۱) اطمینان از وجود دسته‌بندی‌های تکمیلی
        var extraCats = new[]
        {
            ("زیبایی و سلامت", "beauty", "💄", 5),
            ("ورزش و سفر", "sports", "⚽", 6)
        };
        foreach (var (name, slug, icon, sort) in extraCats)
        {
            if (!await db.Categories.AnyAsync(c => c.Slug == slug))
            {
                db.Categories.Add(new Category
                {
                    Name = name, Slug = slug, Icon = icon, IsActive = true, SortOrder = sort,
                    SeoTitle = $"خرید {name} | نوین‌شاپ",
                    SeoDescription = $"بهترین محصولات {name} با ضمانت اصالت و ارسال سریع"
                });
            }
        }
        await db.SaveChangesAsync();

        var catMap = await db.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);

        // ۲) افزودن محصولات دموی کامل (تضمین حداقل ۶ تا ۸ محصول در هر دسته‌بندی برای پر بودن اسلایدرهای ۵ تایی)
        if (await db.Products.CountAsync() < 40)
        {
            var existingSlugs = (await db.Products.Select(p => p.Slug).ToListAsync()).ToHashSet();
            var demoProducts = new (string Name, string CatSlug, int Price, int? OldPrice, int Stock, string ShortDesc, string HtmlDesc)[]
            {
                ("گوشی موبایل گلکسی S24 اولترا ظرفیت ۲۵۶ گیگابایت", "digital", 64_900_000, 68_500_000, 7,
                 "پرچمدار سامسونگ با بدنه تیتانیومی، قلم S-Pen، دوربین ۲۰۰ مگاپیکسلی و نمایشگر Dynamic AMOLED 2X",
                 "<h2>معرفی گلکسی S24 Ultra</h2><p>گوشی هوشمند <strong>Galaxy S24 Ultra</strong> با فریم تیتانیومی مقاوم و پردازنده قدرتمند Snapdragon 8 Gen 3 تجربه بی‌نظیری از سرعت و عکاسی حرفه‌ای را فراهم می‌کند.</p><table><thead><tr><th>ویژگی</th><th>مشخصات</th></tr></thead><tbody><tr><td>نمایشگر</td><td>۶.۸ اینچ QHD+ Dynamic AMOLED 2X با نرخ ۱۲۰ هرتز</td></tr><tr><td>دوربین اصلی</td><td>۲۰۰ مگاپیکسل + زوم اپتیکال ۵ برابری</td></tr><tr><td>باتری</td><td>۵۰۰۰ میلی‌آمپرساعت با شارژ سریع ۴۵ وات</td></tr></tbody></table>"),

                ("لپ‌تاپ ۱۵.۶ اینچی ایسوس مدل VivoBook 15 پردازنده Core i5", "digital", 28_400_000, 31_200_000, 9,
                 "لپ‌تاپ سبک و کارآمد با پردازنده نسل ۱۳ اینتل، رم ۱۶ گیگابایت و حافظه ۵۱۲ گیگابایت SSD",
                 "<h2>بررسی تخصصی لپ‌تاپ ایسوس VivoBook 15</h2><p>گزینه‌ای ایده‌آل برای برنامه‌نویسی، کارهای روزمره، دانشگاه و امور گرافیکی سبک با طراحی باریک و کیبورد ارگونومیک.</p><ul><li>پردازنده: Intel Core i5-1335U</li><li>حافظه رم: ۱۶ گیگابایت DDR4</li><li>حافظه داخلی: ۵۱۲ گیگابایت NVMe SSD</li></ul>"),

                ("پاوربانک ۲۰۰۰۰ میلی‌آمپرساعت فست‌شارژ ۲۲.۵ وات", "digital", 1_190_000, 1_480_000, 25,
                 "پاوربانک ظرفیت بالا با خروجی PD و QC3.0، نمایشگر دیجیتال درصد شارژ و قابلیت شارژ همزمان ۳ دستگاه",
                 "<h2>پاوربانک فست‌شارژ ۲۰۰۰۰</h2><p>مجهز به تراشه ایمنی محافظت در برابر نوسان برق و اتصال کوتاه، مناسب سفر و استفاده روزمره.</p>"),

                ("کیبورد مکانیکال گیمینگ RGB کلید قرمز", "digital", 2_150_000, 2_600_000, 14,
                 "کیبورد مکانیکال با سوییچ‌های خطی نرم، نورپردازی RGB قابل تنظیم و بدنه آلومینیومی مقاوم",
                 "<h2>کیبورد مکانیکال حرفه‌ای</h2><p>دارای قابلیت Anti-Ghosting کامل کلیدها و عمر مفید ۵۰ میلیون ضربه برای گیمرها و تایپیست‌های حرفه‌ای.</p>"),

                ("ماوس بی‌سیم ارگونومیک سایلنت مدل Pro", "digital", 690_000, null, 35,
                 "ماوس دوحالته بلوتوث و دانگل USB با کلیک‌های بی‌صدا و دقت ۲۴۰۰ DPI",
                 "<p>طراحی استاندارد برای جلوگیری از خستگی مچ دست در استفاده طولانی‌مدت.</p>"),

                ("کنسول بازی سونی مدل PlayStation 5 Slim ظرفیت ۱ ترابایت", "digital", 32_500_000, 34_900_000, 6,
                 "نسخه اسلیم پلی‌استیشن ۵ با حافظه ۱ ترابایت SSD فوق سریع و کنترلر بی‌سیم DualSense",
                 "<h2>کنسول PlayStation 5 Slim</h2><p>اجرای بازی‌های نسل نهم با کیفیت 4K و نرخ ۱۲۰ فریم بر ثانیه همراه با فناوری صدای سه‌بعدی Tempest 3D.</p>"),

                ("تبلت ۱۱ اینچی سامسونگ مدل Galaxy Tab S9 FE با قلم S-Pen", "digital", 21_800_000, 23_500_000, 8,
                 "تبلت هوشمند مجهز به نمایشگر ۹۰ هرتزی، قلم نوری داخل جعبه و گواهی ضدآب IP68",
                 "<p>مناسب یادداشت‌برداری، طراحی دیجیتال، مطالعه و تماشای فیلم با باتری ۸۰۰۰ میلی‌آمپرساعت.</p>"),

                ("کتانی مخصوص پیاده‌روی و دویدن مدل AirRun", "fashion", 1_650_000, 2_100_000, 18,
                 "کفش ورزشی فوق سبک با کفی طبی مموری‌فوم و رویه تنفس‌پذیر ضدتعریق",
                 "<h2>کفش پیاده‌روی AirRun</h2><p>رویه بافت اسپیسر با گردش هوای عالی و زیره EVA ضدسایش و منعطف.</p>"),

                ("کوله‌پشتی لپ‌تاپ ضدآب مدل Urban 15.6", "fashion", 1_120_000, 1_390_000, 22,
                 "کوله‌پشتی روزمره و اداری با محفظه ضربه‌گیر لپ‌تاپ، درگاه USB خارجی و پارچه برزنتی ضدآب",
                 "<p>دارای بندهای طبی اسفنجی و جیب مخفی ضدسرقت در بخش پشتی کوله.</p>"),

                ("عینک آفتابی پلاریزه با محافظت UV400", "fashion", 890_000, 1_180_000, 11,
                 "عینک آفتابی فریم کائوچویی سبک با عدسی پلاریزه ضدبازتاب نور همراه با کیف چرمی",
                 "<p>مناسب رانندگی و استفاده روزمره با حذف کامل اشعه‌های مضر خورشید.</p>"),

                ("پیراهن مردانه کتان آستین‌بلند چهارخانه", "fashion", 640_000, null, 28,
                 "پیراهن مردانه دوخت ممتاز از الیاف ۱۰۰٪ نخ‌پنبه بدون آبرفت و رنگ‌دهی",
                 "<p>تن‌خور عالی و راحت مناسب چهارفصل.</p>"),

                ("کاپشن مردانه ضدآب و بادگیر چهارفصل مدل Alpine", "fashion", 2_390_000, 2_850_000, 12,
                 "کاپشن سبک با رویه واترپروف، آستر پلار گرم و کلاه قابل جداسازی",
                 "<p>مناسب روزهای بارانی، سرد و سفرهای پاییزی و زمستانی.</p>"),

                ("ست کیف پول و کمربند چرم طبیعی دست‌دوز", "fashion", 980_000, 1_250_000, 16,
                 "ست هدیه چرم گاوی طبیعی شامل کیف پول جیبی جادار، جاکارتی و کمربند سگک استیل",
                 "<p>عرضه‌شده در جعبه چوبی نفیس، گزینه‌ای عالی برای هدیه.</p>"),

                ("ساعت مچی عقربه‌ای مردانه بند استیل کرنوگراف", "fashion", 3_150_000, 3_690_000, 9,
                 "ساعت مچی کلاسیک موتور ژاپن با شیشه کریستال ضدخش و مقاومت در برابر آب تا ۳ اتمسفر",
                 "<p>دارای تقویم روزشمار و شب‌نما روی عقربه‌ها.</p>"),

                ("سرخ‌کن بدون روغن دیجیتال ۶.۵ لیتری", "home", 4_750_000, 5_600_000, 8,
                 "هواپز و سرخ‌کن رژیمی کم‌مصرف با ۸ برنامه پخت لمسی و سبد نچسب قابل شست‌وشو",
                 "<h2>سرخ‌کن رژیمی ۶.۵ لیتری</h2><p>پخت انواع سیب‌زمینی، مرغ، استیک و شیرینی با <strong>۸۵٪ چربی کمتر</strong> و گردش هوای ۳۶۰ درجه.</p>"),

                ("جاروبرقی رباتیک هوشمند با قابلیت تی‌کشی", "home", 12_900_000, 14_800_000, 4,
                 "جارو رباتیک مجهز به سنسور لیزری LiDAR، مکش ۴۰۰۰ پاسکال و کنترل از طریق موبایل",
                 "<p>نقشه‌برداری دقیق از منزل و بازگشت خودکار به پایه شارژ.</p>"),

                ("چراغ مطالعه رومیزی شارژی لمسی مدل EyeCare", "home", 580_000, 740_000, 19,
                 "چراغ مطالعه LED بدون پرش نور با ۳ حالت رنگ نور، دیمر لمسی و گردن قابل انعطاف",
                 "<p>باتری داخلی لیتیومی با شارژدهی تا ۸ ساعت مطالعه مداوم.</p>"),

                ("سرویس قابلمه ۶ پارچه گرانیتی نچسب", "home", 3_250_000, 3_890_000, 6,
                 "ست قابلمه و تابه گرانیتی با پوشش نچسب چندلایه، درب پیرکس سوپاپ‌دار و پخش یکنواخت حرارت",
                 "<p>فاقد مواد مضر PFOA و مقاوم در برابر خط و خش.</p>"),

                ("چای‌ساز و کتری برقی هوشمند استیل مدل TeaMaster", "home", 2_680_000, 3_100_000, 11,
                 "چای‌ساز روهمی با کتری ۲ لیتری استیل، قوری پیرکس مدرج و سیستم گرم‌نگهدارنده خودکار",
                 "<p>جوش آمدن سریع آب و خاموشی خودکار در صورت اتمام آب کتری.</p>"),

                ("مخلوط‌کن و آسیاب حرفه‌ای ۸۰۰ وات چهارتیغه", "home", 1_950_000, 2_350_000, 14,
                 "مخلوط‌کن پارچ شیشه‌ای نشکن ۱.۵ لیتری با تیغه‌های استیل ضدزنگ مناسب اسموتی و یخ‌شکن",
                 "<p>همراه با آسیاب مجزا برای قهوه، زعفران و مغزیجات.</p>"),

                ("کتاب عادت‌های اتمی اثر جیمز کلیر", "books", 245_000, 295_000, 50,
                 "پرفروش‌ترین کتاب نیویورک‌تایمز درباره ساخت عادت‌های خوب و ترک عادت‌های بد با تغییرات کوچک روزانه",
                 "<p>ترجمه روان به همراه کاغذ بالکی کرم سبک و جلد شومیز سلفون مات.</p>"),

                ("کتاب طراحی سیستم‌های داده محور (Designing Data-Intensive Applications)", "books", 620_000, 750_000, 15,
                 "مرجع جامع معماری نرم‌افزار، پایگاه‌های داده، سیستم‌های توزیع‌شده و مقیاس‌پذیری",
                 "<p>یکی از ارزشمندترین کتاب‌های مهندسی نرم‌افزار و معماری بک‌اند برای توسعه‌دهندگان حرفه‌ای.</p>"),

                ("کتاب هنر شفاف اندیشیدن اثر رولف دوبلی", "books", 210_000, null, 35,
                 "شناخت ۹۹ خطای شناختی رایج ذهن در تصمیم‌گیری‌های زندگی و کسب‌وکار",
                 "<p>کتابی کاربردی و خواندنی برای ارتقای تفکر انتقادی.</p>"),

                ("کتاب کار عمیق (Deep Work) اثر کال نیوپورت", "books", 230_000, 275_000, 32,
                 "قوانین تمرکز طلایی و بهره‌وری بالا در دنیای پر از حواس‌پرتی‌های دیجیتال",
                 "<p>راهنمای عملی برای برنامه‌نویسان، پژوهشگران و مدیران جهت یادگیری سریع مهارت‌های پیچیده.</p>"),

                ("کتاب روانشناسی پول اثر مورگان هاوزل", "books", 215_000, 260_000, 40,
                 "درس‌های ماندگار درباره ثروت، طمع، سرمایه‌گذاری هوشمندانه و رضایت در زندگی مالی",
                 "<p>یکی از پرفروش‌ترین کتاب‌های مالی و رفتاری جهان با ۱۹ داستان کوتاه و آموزنده.</p>"),

                ("کتاب کد تمیز (Clean Code) اثر رابرت سی مارتین", "books", 540_000, 640_000, 18,
                 "مرجع کلاسیک اصول کدنویسی تمیز، بازآرایی کد (Refactoring) و مهندسی نرم‌افزار چابک",
                 "<p>ترجمه دقیق فارسی همراه با نمونه کدهای استاندارد و خوانا.</p>"),

                ("ست برنامه‌ریزی روزانه و دفتر یادداشت چرمی نقطه‌ای (بولت ژورنال)", "books", 340_000, 410_000, 26,
                 "دفتر ۱۶۰ برگ کاغذ نخودی ۱۰۰ گرم بدون پس‌دادن جوهر همراه با روان‌نویس نوك نمدی",
                 "<p>دارای کش نگهدارنده، روبان نشانگر و جیب داخلی در جلد پشتی.</p>"),

                ("سرم ویتامین C روشن‌کننده و ضدلک پوست ۳۰ میل", "beauty", 460_000, 590_000, 24,
                 "سرم تخصصی حاوی ویتامین C پایدار و هیالورونیک اسید برای شادابی، آبرسانی و کاهش لک‌های پوستی",
                 "<p>مناسب انواع پوست، جذب سریع بدون ایجاد حس چربی.</p>"),

                ("مسواک برقی شارژی سونیک به همراه ۳ سری یدک", "beauty", 1_380_000, 1_750_000, 10,
                 "مسواک برقی با ۴۰۰۰۰ لرزش در دقیقه، تایمر هوشمند ۲ دقیقه‌ای و بدنه کاملاً ضدآب IPX7",
                 "<p>دارای ۵ حالت تمیزکنندگی شامل سفیدکننده، مراقبت از لثه و دندان‌های حساس.</p>"),

                ("سشوار حرفه‌ای سالنی ۲۴۰۰ وات موتور AC", "beauty", 1_850_000, 2_200_000, 7,
                 "سشوار پرقدرت با فناوری تولید یون منفی برای جلوگیری از وز شدن مو و ۲ متمرکزکننده باد",
                 "<p>سیم بلند ۲.۵ متری و فیلتر پشتی قابل باز شدن و تمیزکاری.</p>"),

                ("کرم ضدآفتاب و آبرسان بی‌رنگ SPF50 مناسب انواع پوست", "beauty", 390_000, 470_000, 30,
                 "ضدآفتاب فلوئیدی فوق سبک با محافظت کامل UVA/UVB بدون ایجاد رد سفیدی و چربی روی پوست",
                 "<p>غنی‌شده با عصاره آلوئه‌ورا و نیاسینامید.</p>"),

                ("ماشین اصلاح و خط‌زن شارژی ضدآب بدنه فلزی", "beauty", 1_190_000, 1_450_000, 14,
                 "ماشین اصلاح صورت و مو با تیغه تیتانیومی خودتیزشونده، نمایشگر LED باتری و ۴ شانه اصلاح",
                 "<p>شارژدهی ۱۸۰ دقیقه‌ای با درگاه Type-C.</p>"),

                ("ادوپرفیوم چهارفصل خنک و تلخ ۱۰۰ میل مدل Bleu Intense", "beauty", 1_680_000, 2_050_000, 9,
                 "عطر با ماندگاری و پخش بوی بالا از نت‌های مرکباتی، چوب صندل و کهربا",
                 "<p>مناسب استفاده روزانه و مجالس رسمی.</p>"),

                ("زیرانداز ورزشی یوگا و پیلاتس ضدلغزش TPE", "sports", 680_000, 850_000, 20,
                 "مت یوگا دولایه با ضخامت ۸ میلی‌متر، بافت ضدتعریق و کاور حمل دوشی",
                 "<p>ساخته‌شده از مواد دوستدار محیط‌زیست بدون بو و حساسیت.</p>"),

                ("قمقمه ورزشی دوجداره استیل حفظ دما ۷۵۰ میل", "sports", 520_000, 650_000, 16,
                 "فلاسک و قمقمه استیل ضدزنگ ۳۰۴ با نگهداری آب سرد تا ۲۴ ساعت و نوشیدنی گرم تا ۱۲ ساعت",
                 "<p>درب واشر‌دار ۱۰۰٪ ضدنشت مناسب باشگاه، کوهنوردی و سفر.</p>"),

                ("چادر مسافرتی ۴ نفره ضدآب اتوماتیک", "sports", 2_890_000, 3_450_000, 5,
                 "چادر کمپینگ فنری با نصب سریع، پارچه پلی‌استر ضدآب و پنجره‌های توری ضدحشره",
                 "<p>همراه با کیف حمل برزنتی و میخ‌های تثبیت زمین.</p>"),

                ("ست دمبل متغیر و هالتر خانگی ۲۰ کیلوگرمی با کیف حمل", "sports", 2_180_000, 2_550_000, 8,
                 "مجموعه دمبل قابل تنظیم با روکش PVC ضدضربه و میله رابط تبدیل به هالتر",
                 "<p>مناسب تمرینات بدنسازی و فیتنس در منزل.</p>"),

                ("مجموعه ۵ عددی کش ورزشی مقاومتی لوپ به همراه کیسه حمل", "sports", 360_000, 450_000, 28,
                 "ست کش‌های ورزشی لاتکس طبیعی در ۵ سطح مقاومت مختلف از سبک تا فوق سنگین",
                 "<p>ایده‌آل برای گرم کردن، فیزیوتراپی، پیلاتس و تمرینات قدرتی.</p>"),

                ("کیسه خواب کوهنوردی و کمپینگ ضدآب مدل ThermoLight", "sports", 1_490_000, 1_780_000, 11,
                 "کیسه خواب سبک و کم‌حجم با الیاف هالوغرام حفظ گرما تا دمای منفی ۵ درجه",
                 "<p>دارای کاور فشرده‌سازی و کلاه ضدباد قابل تنظیم.</p>")
            };

            foreach (var p in demoProducts)
            {
                var slug = SlugHelper.Generate(p.Name);
                if (existingSlugs.Contains(slug) || !catMap.TryGetValue(p.CatSlug, out var catId)) continue;
                existingSlugs.Add(slug);

                db.Products.Add(new Product
                {
                    Name = p.Name,
                    Slug = slug,
                    CategoryId = catId,
                    Price = p.Price,
                    OldPrice = p.OldPrice,
                    Stock = p.Stock,
                    ShortDescription = p.ShortDesc,
                    Description = p.HtmlDesc,
                    IsActive = true,
                    SeoTitle = $"خرید {p.Name} | نوین‌شاپ"[..Math.Min(70, $"خرید {p.Name} | نوین‌شاپ".Length)],
                    SeoDescription = p.ShortDesc[..Math.Min(170, p.ShortDesc.Length)]
                });
            }
            await db.SaveChangesAsync();
        }

        // ۳) افزودن بنرهای اسلایدر صفحه اصلی (اگر هنوز بنری ثبت نشده باشد)
        if (!await db.Banners.AnyAsync())
        {
            db.Banners.AddRange(
                new Banner
                {
                    Title = "جشنواره شگفت‌انگیز کالای دیجیتال",
                    Subtitle = "تا ۳۰٪ تخفیف ویژه روی موبایل، لپ‌تاپ، هدفون و ساعت‌های هوشمند با ارسال فوری",
                    ButtonText = "مشاهده تخفیف‌های دیجیتال",
                    LinkUrl = "/category/digital",
                    IsActive = true,
                    SortOrder = 1
                },
                new Banner
                {
                    Title = "تجهیز خانه و آشپزخانه با بهترین قیمت",
                    Subtitle = "جدیدترین لوازم برقی، قهوه‌ساز و سرخ‌کن‌های رژیمی با ضمانت اصالت کالا",
                    ButtonText = "خرید لوازم خانه",
                    LinkUrl = "/category/home",
                    IsActive = true,
                    SortOrder = 2
                },
                new Banner
                {
                    Title = "استایل جدید فصل با کلکسیون پوشاک و اکسسوری",
                    Subtitle = "ارسال رایگان برای تمامی سفارش‌های بالای ۱ میلیون تومان به سراسر کشور",
                    ButtonText = "مشاهده کلکسیون پوشاک",
                    LinkUrl = "/category/fashion",
                    IsActive = true,
                    SortOrder = 3
                }
            );
            await db.SaveChangesAsync();
        }

        // ۴) تنظیم تایمر پیشنهاد شگفت‌انگیز در صورت خالی بودن
        if (!await db.SiteSettings.AnyAsync(s => s.Key == "deals.ends"))
        {
            var endStr = Fmt.TehranNow().AddDays(5).ToString("yyyy-MM-dd 23:59", System.Globalization.CultureInfo.InvariantCulture);
            db.SiteSettings.Add(new SiteSetting { Key = "deals.ends", Value = endStr });
            await db.SaveChangesAsync();
        }

        // ۵) ثبت چند سفارش پرداخت‌شده نمونه تا سکشن «پرفروش‌ترین‌ها» و نمودار داشبورد پر و واقعی دیده شوند
        if (!await db.Orders.AnyAsync(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped))
        {
            var prods = await db.Products.AsNoTracking().OrderBy(p => p.Id).Take(8).ToListAsync();
            if (prods.Count >= 4)
            {
                var sampleOrders = new[]
                {
                    ("NS-DEMO1001", "محمد امینی", "09121112233", "تهران، خیابان ولیعصر، پلاک ۱۲۰", OrderStatus.Shipped, "zarinpal", "ZP-84920144", -3, new[] { (prods[0], 2), (prods[2], 1) }),
                    ("NS-DEMO1002", "سارا رضایی", "09123334455", "اصفهان، چهارباغ بالا، مجتمع پارک", OrderStatus.Paid, "zarinpal", "ZP-99201831", -2, new[] { (prods[1], 1), (prods[3], 2) }),
                    ("NS-DEMO1003", "امیرحسین کاظمی", "09356667788", "شیراز، بلوار ستارخان، کوچه ۱۲", OrderStatus.Paid, "card_to_card", "C2C-APPROVED", -1, new[] { (prods[4 % prods.Count], 3), (prods[5 % prods.Count], 1) }),
                    ("NS-DEMO1004", "نگار حسینی", "09198889900", "مشهد، بلوار وکیل‌آباد، پلاک ۴۵", OrderStatus.Paid, "zarinpal", "ZP-77412098", 0, new[] { (prods[6 % prods.Count], 2), (prods[7 % prods.Count], 1) })
                };

                foreach (var (code, cust, phone, addr, st, method, pref, dayOffset, lines) in sampleOrders)
                {
                    var sub = lines.Sum(l => (long)l.Item1.Price * l.Item2);
                    var ship = sub >= 1_000_000 ? 0L : 50_000L;
                    var created = DateTime.UtcNow.AddDays(dayOffset).AddHours(-2);
                    db.Orders.Add(new Order
                    {
                        Code = code,
                        CustomerName = cust,
                        Phone = phone,
                        Address = addr,
                        Status = st,
                        PaymentMethod = method,
                        PaymentRef = pref,
                        CreatedAt = created,
                        PaidAt = created.AddMinutes(10),
                        Subtotal = sub,
                        Shipping = ship,
                        Total = sub + ship,
                        Items = lines.Select(l => new OrderItem
                        {
                            ProductId = l.Item1.Id,
                            ProductName = l.Item1.Name,
                            UnitPrice = l.Item1.Price,
                            Quantity = l.Item2
                        }).ToList()
                    });
                }
                await db.SaveChangesAsync();
            }
        }

        // ۶) کدهای تخفیف نمونه برای تست فوری در سبد خرید و صفحه تسویه‌حساب
        if (!await db.Coupons.AnyAsync())
        {
            db.Coupons.AddRange(
                new Coupon
                {
                    Code = "WELCOME15",
                    Title = "تخفیف ۱۵٪ خوش‌آمدگویی (تا سقف ۳۰۰ هزار تومان)",
                    IsPercent = true,
                    Value = 15,
                    MaxDiscount = 300_000,
                    MinOrderAmount = 200_000,
                    UsageLimit = 100,
                    IsActive = true
                },
                new Coupon
                {
                    Code = "NOVIN100",
                    Title = "۱۰۰ هزار تومان هدیه خرید بالای ۵۰۰ هزار تومان",
                    IsPercent = false,
                    Value = 100_000,
                    MinOrderAmount = 500_000,
                    UsageLimit = 50,
                    IsActive = true
                },
                new Coupon
                {
                    Code = "VIP20",
                    Title = "تخفیف ویژه ۲۰٪ سفارش‌های بالای ۲ میلیون تومان",
                    IsPercent = true,
                    Value = 20,
                    MaxDiscount = 1_000_000,
                    MinOrderAmount = 2_000_000,
                    UsageLimit = 30,
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
        }

        // ۷) تکمیل خودکار جدول مشخصات فنی ساختاریافته برای محصولات دمو
        var productsWithoutSpecs = await db.Products.Include(p => p.Category)
            .Where(p => p.SpecsJson == null || p.SpecsJson == "")
            .ToListAsync();
        if (productsWithoutSpecs.Count > 0)
        {
            foreach (var p in productsWithoutSpecs)
            {
                var catName = p.Category?.Name ?? "";
                var specs = BuildSampleSpecs(p.Name, catName);
                p.SpecsJson = System.Text.Json.JsonSerializer.Serialize(specs);
            }
            await db.SaveChangesAsync();
        }

        // ۸) ثبت دیدگاه‌ها و امتیازات نمونه روی محصولات برای غنی‌تر شدن دمو
        if (!await db.ProductReviews.AnyAsync())
        {
            var allProds = await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
            var sampleReviews = new List<ProductReview>();
            var reviewers = new[]
            {
                ("مهدی رضوانی", 5, "ارزش خرید بسیار بالا نسبت به قیمت", "بسته‌بندی کاملاً پلمپ و اورجینال بود و ظرف ۲۴ ساعت به دستم رسید. کیفیت ساخت واقعاً فراتر از انتظارم بود.", "کیفیت ساخت عالی، ارسال سریع، قیمت منصفانه", null, true, "سپاس از اعتماد و همراهی شما با نوین‌شاپ؛ مبارکتان باشد 🌹"),
                ("نازنین شریفی", 5, "کاملاً مطابق توضیحات سایت", "از پشتیبانی قبل از خرید راهنمایی گرفتم و دقیقاً همون کیفیتی بود که توضیح داده بودن. به دوستان هم پیشنهاد می‌کنم.", "طراحی شیک، کارایی عالی، گارانتی معتبر", null, true, null),
                ("علیرضا کریمی", 4, "کیفیت خوب و رضایت‌بخش", "در کل از خریدم راضی هستم و نسبت به نمونه‌های مشابه در بازار کیفیت بهتری داره.", "دوام بالا، عملکرد روان", "کاش رنگ‌بندی بیشتری داشت", true, "ممنون از ثبت نظر ارزشمندتان؛ تنوع رنگ در پارت بعدی انبار افزایش خواهد یافت.")
            };

            for (int i = 0; i < allProds.Count; i++)
            {
                var prod = allProds[i];
                var r1 = reviewers[i % reviewers.Length];
                var r2 = reviewers[(i + 1) % reviewers.Length];

                sampleReviews.Add(new ProductReview
                {
                    ProductId = prod.Id,
                    AuthorName = r1.Item1,
                    Rating = r1.Item2,
                    Title = r1.Item3,
                    Comment = r1.Item4,
                    Pros = r1.Item5,
                    Cons = r1.Item6,
                    IsBuyer = r1.Item7,
                    IsApproved = true,
                    AdminReply = r1.Item8,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i % 7 + 1))
                });

                sampleReviews.Add(new ProductReview
                {
                    ProductId = prod.Id,
                    AuthorName = r2.Item1,
                    Rating = r2.Item2,
                    Title = r2.Item3,
                    Comment = r2.Item4,
                    Pros = r2.Item5,
                    Cons = r2.Item6,
                    IsBuyer = r2.Item7,
                    IsApproved = true,
                    AdminReply = r2.Item8,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i % 5 + 2))
                });
            }

            db.ProductReviews.AddRange(sampleReviews);
            await db.SaveChangesAsync();
        }

        // ۹) تکمیل قیمت خرید واحد (CostPrice) محصولات و ثبت کاردکس اولیه انبار و اسناد حسابداری دمو
        var prodsNoCost = await db.Products.Where(p => p.CostPrice == 0).ToListAsync();
        if (prodsNoCost.Count > 0)
        {
            foreach (var p in prodsNoCost)
            {
                // قیمت خرید عمده حدود ۷۲٪ تا ۷۸٪ قیمت فروش (رند شده به هزار تومان)
                p.CostPrice = Math.Max(1000, ((int)Math.Round(p.Price * 0.74 / 1000.0)) * 1000);
            }
            await db.SaveChangesAsync();
        }

        var itemsNoCost = await db.OrderItems.Where(i => i.UnitCost == 0).ToListAsync();
        if (itemsNoCost.Count > 0)
        {
            var costMap = await db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.CostPrice);
            foreach (var item in itemsNoCost)
            {
                item.UnitCost = costMap.TryGetValue(item.ProductId, out var c) && c > 0
                    ? c
                    : Math.Max(1000, ((int)Math.Round(item.UnitPrice * 0.74 / 1000.0)) * 1000);
            }
            await db.SaveChangesAsync();
        }

        var txProdIds = (await db.InventoryTransactions.Select(t => t.ProductId).Distinct().ToListAsync()).ToHashSet();
        var missingTxProducts = await db.Products.AsNoTracking().Where(p => !txProdIds.Contains(p.Id)).OrderBy(p => p.Id).ToListAsync();
        if (missingTxProducts.Count > 0)
        {
            var initTxs = new List<InventoryTransaction>();
            foreach (var p in missingTxProducts)
            {
                var cost = p.CostPrice > 0 ? p.CostPrice : Math.Max(1000, ((int)Math.Round(p.Price * 0.74 / 1000.0)) * 1000);
                initTxs.Add(new InventoryTransaction
                {
                    ProductId = p.Id,
                    Type = InventoryTxType.StockIn,
                    QuantityChange = p.Stock,
                    StockAfter = p.Stock,
                    UnitCost = cost,
                    Reference = $"INV-INIT-{100 + p.Id}",
                    Note = "موجودی اولیه ورود به انبار مرکزی",
                    CreatedBy = "admin@novinshop.local",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                });
            }
            db.InventoryTransactions.AddRange(initTxs);
            await db.SaveChangesAsync();
        }

        if (!await db.AccountingEntries.AnyAsync())
        {
            db.AccountingEntries.AddRange(
                new AccountingEntry
                {
                    Type = AccountingEntryType.Expense,
                    Category = "بسته‌بندی و ارسال مرسولات",
                    Title = "خرید کارتن پستی ۳ لایه، نایلون حباب‌دار و چسب پهن اختصاصی",
                    Amount = 1_850_000,
                    Reference = "EXP-1401",
                    Note = "تأمین ملزومات بسته‌بندی انبار برای ۲۰۰ مرسوله",
                    CreatedBy = "admin@novinshop.local",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new AccountingEntry
                {
                    Type = AccountingEntryType.Expense,
                    Category = "تبلیغات و بازاریابی",
                    Title = "کمپین تبلیغات کلیکی و بنری جشنواره فروش ویژه",
                    Amount = 3_200_000,
                    Reference = "EXP-1402",
                    Note = "تبلیغات یک‌ماهه جذب مشتری",
                    CreatedBy = "admin@novinshop.local",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new AccountingEntry
                {
                    Type = AccountingEntryType.Income,
                    Category = "درآمد خدمات و متفرقه",
                    Title = "درآمد خدمات بسته‌بندی کادویی و ارسال اکسپرس ویژه",
                    Amount = 950_000,
                    Reference = "INC-2101",
                    Note = "درآمد خدمات ارزش افزوده سفارش‌های ویژه",
                    CreatedBy = "admin@novinshop.local",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            );
            await db.SaveChangesAsync();
        }

        // ۸) مقالات وبلاگ و مجله فروشگاه
        if (!await db.BlogPosts.AnyAsync())
        {
            var samplePosts = new[]
            {
                new BlogPost
                {
                    Title = "پرچمداران جدید موبایل در سال جاری از چه فناوری‌هایی بهره خواهند برد؟",
                    Slug = "new-flagship-smartphones-tech-guide",
                    Category = "تکنولوژی",
                    Icon = "📱",
                    Gradient = "linear-gradient(135deg,#1e3a8a,#0f172a)",
                    ReadingMinutes = 6,
                    ViewCount = 840,
                    Summary = "بررسی جامع نسل جدید پردازنده‌های ۳ نانومتری، هوش مصنوعی پردازش تصویر، باتری‌های سیلیکون-کربن و نمایشگرهای کم‌مصرف در گوشی‌های پرچمدار جدید.",
                    Content = "<h2>نسل جدید گوشی‌های هوشمند چه تغییراتی دارد؟</h2><p>امسال شاهد یکی از بزرگ‌ترین جهش‌های سخت‌افزاری و نرم‌افزاری در دنیای موبایل هستیم. شرکت‌های بزرگ سازنده تراشه با مهاجرت به <strong>لیتوگرافی ۳ نانومتری</strong> توانسته‌اند مصرف انرژی را تا ۳۰ درصد کاهش و توان پردازش عصبی (NPU) را بیش از دو برابر افزایش دهند.</p><h3>۱. باتری‌های سیلیکون-کربن با ظرفیت بالاتر</h3><p>فناوری جدید باتری امکان قرار دادن ظرفیت ۵۵۰۰ تا ۶۰۰۰ میلی‌آمپرساعت را در بدنه‌های باریک زیر ۸ میلی‌متر فراهم کرده است.</p><h3>۲. عکاسی محاسباتی و زوم پریسکوپی</h3><p>ترکیب سنسورهای ۲۰۰ مگاپیکسلی با الگوریتم‌های هوش مصنوعی، عکاسی در شب و زوم‌های ۵ تا ۱۰ برابری را به سطح دوربین‌های حرفه‌ای نزدیک کرده است.</p>",
                    CreatedAt = Fmt.TehranNow().AddDays(-1)
                },
                new BlogPost
                {
                    Title = "راهنمای جامع خرید هدفون بی‌سیم و بررسی فناوری حذف نویز فعال (ANC)",
                    Slug = "wireless-headphones-anc-buying-guide",
                    Category = "صوتی و تصویری",
                    Icon = "🎧",
                    Gradient = "linear-gradient(135deg,#4c1d95,#1e1b4b)",
                    ReadingMinutes = 5,
                    ViewCount = 620,
                    Summary = "چگونه بهترین هدفون یا هندزفری بلوتوثی را متناسب با کاربری روزمره، ورزش، مکالمه یا موسیقی انتخاب کنیم؟ تفاوت ANC هیبریدی و غیرفعال چیست؟",
                    Content = "<h2>فناوری حذف نویز فعال (ANC) چگونه کار می‌کند؟</h2><p>هدفون‌های مجهز به <strong>Active Noise Cancelling</strong> با استفاده از میکروفون‌های محیطی، صدای مزاحم اطراف را دریافت کرده و موج صوتی معکوس تولید می‌کنند تا صدای محیط خنثی شود.</p><ul><li><strong>هدفون‌های دورگوشی (Over-Ear):</strong> بهترین گزینه برای تمرکز طولانی، سفر و استودیو با شارژدهی ۳۰ تا ۴۰ ساعت.</li><li><strong>ایربادهای ترووایرلس (TWS):</strong> سبک، جیبی و مقاوم در برابر تعریق، ایده‌آل برای باشگاه و پیاده‌روی.</li></ul>",
                    CreatedAt = Fmt.TehranNow().AddDays(-2)
                },
                new BlogPost
                {
                    Title = "رسیدن به بهترین نتیجه در کمترین زمان با تکنیک کار عمیق و برنامه‌ریزی",
                    Slug = "deep-work-and-daily-planning-guide",
                    Category = "کتاب و سبک زندگی",
                    Icon = "📋",
                    Gradient = "linear-gradient(135deg,#78350f,#451a03)",
                    ReadingMinutes = 7,
                    ViewCount = 510,
                    Summary = "مروری بر مهم‌ترین آموزه‌های کتاب‌های «کار عمیق» و «عادت‌های اتمی» برای مدیریت زمان، حذف عوامل حواس‌پرتی و افزایش بهره‌وری روزانه.",
                    Content = "<h2>چرا تمرکز عمیق ارزشمندترین مهارت قرن است؟</h2><p>کال نیوپورت در کتاب <strong>کار عمیق (Deep Work)</strong> نشان می‌دهد که توانایی تمرکز بدون وقفه روی یک مسئله دشوار، کلید یادگیری سریع و خلق خروجی‌های ماندگار است.</p><blockquote>«تغییرات کوچک ۱ درصدی در هر روز، در پایان سال شما را ۳۷ برابر بهتر از نقطه شروع خواهد کرد.» — جیمز کلیر</blockquote><p>با اختصاص ۲ بلوک ۹۰ دقیقه‌ای بدون اعلان‌های موبایل در ابتدای روز، می‌توانید کارهای اصلی هفته را در نیمی از زمان معمول به پایان برسانید.</p>",
                    CreatedAt = Fmt.TehranNow().AddDays(-3)
                },
                new BlogPost
                {
                    Title = "مقایسه کنسول‌های نسل نهم و راهنمای انتخاب مانیتور گیمینگ مناسب",
                    Slug = "next-gen-console-and-gaming-monitor-guide",
                    Category = "بازی و سرگرمی",
                    Icon = "🎮",
                    Gradient = "linear-gradient(135deg,#064e3b,#022c22)",
                    ReadingMinutes = 6,
                    ViewCount = 730,
                    Summary = "برای دریافت تصویر 4K با نرخ ۱۲۰ هرتز در پلی‌استیشن ۵ و ایکس‌باکس سری ایکس به چه مانیتور، کابل HDMI 2.1 و تجهیزاتی نیاز داریم؟",
                    Content = "<h2>ویژگی‌های کلیدی یک مانیتور گیمینگ استاندارد</h2><p>اگر قصد خرید مانیتور برای کنسول‌های نسل نهم یا سیستم گیمینگ دارید، سه فاکتور <strong>نرخ نوسازی (Refresh Rate)</strong>، <strong>زمان پاسخ‌دهی (1ms)</strong> و <strong>پشتیبانی از درگاه HDMI 2.1 / VRR</strong> اهمیت بالایی دارند.</p><p>پنل‌های Fast IPS تعادل فوق‌العاده‌ای میان دقت رنگ و سرعت واکنش ایجاد می‌کنند و برای بازی‌های رقابتی و داستانی بهترین انتخاب هستند.</p>",
                    CreatedAt = Fmt.TehranNow().AddDays(-4)
                },
                new BlogPost
                {
                    Title = "برای داشتن آخر هفته‌ای آرام و پرانرژی این نکات کاربردی را از دست ندهید!",
                    Slug = "relaxing-weekend-lifestyle-tips",
                    Category = "سبک زندگی",
                    Icon = "☕",
                    Gradient = "linear-gradient(135deg,#334155,#0f172a)",
                    ReadingMinutes = 4,
                    ViewCount = 490,
                    Summary = "چند عادت ساده از دم کردن قهوه تخصصی در خانه تا مطالعه کتاب و پیاده‌روی در طبیعت که انرژی شما را برای شروع هفته جدید دوچندان می‌کند.",
                    Content = "<h2>ریکاوری ذهن و بدن در پایان هفته</h2><p>استراحت واقعی به معنای ساعت‌ها اسکرول کردن در شبکه‌های اجتماعی نیست؛ بلکه تغییر ریتم فعالیت روزانه با سرگرمی‌های آرامش‌بخش است.</p><ul><li>دم‌آوری یک فنجان قهوه تازه با اسپرسوساز خانگی</li><li>مطالعه ۲۰ صفحه از یک کتاب غیرداستانی یا رمان جذاب</li><li>آماده‌سازی میز کار و برنامه‌ریزی ۳ اولویت اصلی هفته آینده</li></ul>",
                    CreatedAt = Fmt.TehranNow().AddDays(-5)
                },
                new BlogPost
                {
                    Title = "راهنمای خرید سرخ‌کن بدون روغن (هواپز)؛ چه ظرفیتی برای خانواده شما مناسب است؟",
                    Slug = "air-fryer-buying-guide-for-home",
                    Category = "خانه و آشپزخانه",
                    Icon = "🍟",
                    Gradient = "linear-gradient(135deg,#9d174d,#4c0519)",
                    ReadingMinutes = 5,
                    ViewCount = 680,
                    Summary = "بررسی تفاوت سرخ‌کن‌های رژیمی دیجیتال، نحوه گردش هوای ۳۶۰ درجه، مصرف برق و نکات طلایی نگهداری از سبد نچسب.",
                    Content = "<h2>چرا سرخ‌کن بدون روغن به محبوب‌ترین وسیله آشپزخانه تبدیل شده است؟</h2><p>سرخ‌کن‌های رژیمی با گردش سریع هوای داغ، بافت ترد و طلایی غذا را با <strong>۸۵ درصد چربی کمتر</strong> ایجاد می‌کنند. برای خانواده‌های ۳ تا ۵ نفره، مدل‌های دارای مخزن <strong>۶ تا ۶.۵ لیتری</strong> بهترین انتخاب هستند و امکان پخت کامل مرغ یا حجم بالای سیب‌زمینی را در یک مرحله فراهم می‌کنند.</p>",
                    CreatedAt = Fmt.TehranNow().AddDays(-6)
                }
            };
            db.BlogPosts.AddRange(samplePosts);
            await db.SaveChangesAsync();
        }
    }

    private static List<ProductSpec> BuildSampleSpecs(string name, string catName)
    {
        if (name.Contains("لپ‌تاپ") || name.Contains("مک‌بوک"))
            return new()
            {
                new("پردازنده مرکزی", "نسل جدید ۸ هسته‌ای با معماری ۳ نانومتری"),
                new("حافظه رم (RAM)", "۱۶ گیگابایت یکپارچه پرسرعت"),
                new("حافظه داخلی", "۵۱۲ گیگابایت SSD NVMe PCIe 4.0"),
                new("صفحه نمایش", "۱۳.۶ اینچ Liquid Retina با روشنایی ۵۰۰ نیت"),
                new("عمر باتری", "تا ۱۸ ساعت پخش مداوم ویدیو"),
                new("گارانتی و خدمات", "۱۸ ماه گارانتی معتبر شرکتی + تضمین اصالت")
            };
        if (name.Contains("گوشی") || name.Contains("موبایل") || name.Contains("گلکسی"))
            return new()
            {
                new("فناوری صفحه‌نمایش", "Super AMOLED 120Hz با محافظ گوریلا گلس"),
                new("حافظه داخلی و رم", "۲۵۶ گیگابایت حافظه داخلی / ۸ گیگابایت رم"),
                new("دوربین اصلی", "سه‌گانه ۵۰ + ۱۲ + ۵ مگاپیکسل با لرزشگیر اپتیکال OIS"),
                new("ظرفیت باتری", "۵۰۰۰ میلی‌آمپرساعت با فست‌شارژ ۲۵ وات"),
                new("مقاومت در برابر آب", "گواهی استاندارد IP67 (مقاوم در برابر آب و گردوغبار)"),
                new("وضعیت رجیستری", "رجیستر شده همراه با کد فعال‌سازی همتا و ۱۸ ماه گارانتی")
            };
        if (catName.Contains("دیجیتال"))
            return new()
            {
                new("نوع اتصال", "بی‌سیم (بلوتوث نسخه ۵.۳) و باسیم"),
                new("عمر باتری / شارژدهی", "تا ۳۰ ساعت استفاده ترکیبی با محفظه شارژ"),
                new("درگاه ارتباطی", "USB Type-C با قابلیت شارژ سریع"),
                new("سازگاری", "اندروید، iOS، ویندوز و macOS"),
                new("جنس بدنه", "آلیاژ آلومینیوم و پلی‌کربنات مقاوم"),
                new("گارانتی", "۱۸ ماه گارانتی تعویض شرکتی")
            };
        if (catName.Contains("پوشاک") || catName.Contains("مد"))
            return new()
            {
                new("جنس الیاف", "نخ پنبه صددرصد ارگانیک ضدحساسیت"),
                new("نوع دوخت و قواره", "قواره استاندارد (Regular Fit) با دوخت صنعتی دوبل"),
                new("مناسب برای", "استفاده روزمره، چهارفصل و استایل کژوال"),
                new("نحوه شست‌وشو", "قابل شست‌وشو با دست و ماشین لباسشویی (حداکثر ۳۰ درجه)"),
                new("کشور تولیدکننده", "ایران (کیفیت صادراتی درجه یک)"),
                new("ضمانت بازگشت", "۷ روز ضمانت تعویض سایز و بازگشت بی‌قید و شرط")
            };
        if (catName.Contains("خانه") || catName.Contains("آشپزخانه"))
            return new()
            {
                new("توان مصرفی / رده انرژی", "مصرف بهینه انرژی با راندمان حرارتی بالا (A++)"),
                new("جنس بدنه و قطعات", "استیل ضدزنگ ۳۰۴ نگیر + پلاستیک فشرده نسوز BPA-Free"),
                new("سیستم ایمنی", "قطع‌کن خودکار حرارتی و محافظ نوسان برق"),
                new("قابلیت شست‌وشو", "قطعات جداشدنی قابل شست‌وشو در ماشین ظرفشویی"),
                new("متعلقات داخل جعبه", "دفترچه راهنمای فارسی، لوازم جانبی کامل و کارت گارانتی"),
                new("گارانتی", "۲۴ ماه ضمانت معتبر خدمات پس از فروش")
            };
        if (catName.Contains("کتاب"))
            return new()
            {
                new("قطع و نوع جلد", "رقعی — جلد شومیز سلفون مات با صحافی چسب گرم"),
                new("نوع کاغذ", "کاغذ بالکی سوئدی کرم‌رنگ (سبک و بدون بازتاب نور)"),
                new("نوبت چاپ", "چاپ جدید (ویراست نهایی و کامل)"),
                new("زبان اثر", "فارسی (ترجمه روان و وفادار به متن اصلی)"),
                new("بسته‌بندی ارسال", "حباب‌پیچ ضدضربه و مقاوم در برابر رطوبت")
            };
        if (catName.Contains("زیبایی") || catName.Contains("سلامت"))
            return new()
            {
                new("مناسب برای", "انواع پوست و مو (تست‌شده به‌صورت درماتولوژیک)"),
                new("ترکیبات شاخص", "فاقد پارابن، سولفات و مواد حساسیت‌زا"),
                new("مجوزها", "دارای پروانه بهداشتی و کد رهگیری سازمان غذا و دارو"),
                new("شرایط نگهداری", "در جای خشک، خنک و دور از تابش مستقیم آفتاب"),
                new("ضمانت اصالت", "۱۰۰٪ اورجینال با هولوگرام اصالت کالا")
            };
        return new()
        {
            new("جنس و متریال", "مواد اولیه درجه یک مقاوم در برابر سایش و رطوبت"),
            new("وزن و قابلیت حمل", "طراحی ارگونومیک، سبک و کم‌حجم همراه با کاور حمل"),
            new("کاربری", "ورزشی، باشگاهی، سفر و طبیعت‌گردی"),
            new("استاندارد ساخت", "تولیدشده مطابق استانداردهای ایمنی و ارگونومی روز"),
            new("ضمانت سلامت", "تضمین سلامت فیزیکی و اصالت کالا + ۷ روز مهلت تست")
        };
    }
}
