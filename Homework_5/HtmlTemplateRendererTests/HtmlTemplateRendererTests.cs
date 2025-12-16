
using System.Text.RegularExpressions;
using TemplateEngine;

namespace TemplateEngine.Tests
{
    [TestClass]
    public class HtmlTemplateRendererTests
    {
        private HtmlTemplateRenderer _renderer = new();

        #region RenderFromString — Variables

        [TestMethod]
        public void RenderFromString_SimpleVariable_ReplacesCorrectly()
        {
            var template = "<h1>Hello, ${Name}!</h1>";
            var model = new { Name = "Alice" };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<h1>Hello, Alice!</h1>", result);
        }

        [TestMethod]
        public void RenderFromString_NestedProperty_ReplacesCorrectly()
        {
            var template = "<p>City: ${Address.City}</p>";
            var model = new { Address = new { City = "Paris" } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<p>City: Paris</p>", result);
        }

        [TestMethod]
        public void RenderFromString_MissingProperty_ReturnsEmpty()
        {
            var template = "<p>${Missing}</p>";
            var model = new { Name = "Test" };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<p></p>", result);
        }

        [TestMethod]
        public void RenderFromString_NullProperty_ReturnsEmpty()
        {
            var template = "<p>${Value}</p>";
            var model = new { Value = (string)null };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<p></p>", result);
        }

        #endregion

        #region RenderFromString — $if

        [TestMethod]
        public void RenderFromString_IfTrue_KeepsTrueBranch()
        {
            var template = "$if(ShowMessage)You see me$endif";
            var model = new { ShowMessage = true };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("You see me", result);
        }

        [TestMethod]
        public void RenderFromString_IfFalse_EmptyResult()
        {
            var template = "$if(ShowMessage)You see me$endif";
            var model = new { ShowMessage = false };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_IfWithElse_TrueBranch()
        {
            var template = "$if(Active)ON$else$OFF$endif";
            var model = new { Active = true };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("ON", result);
        }

        [TestMethod]
        public void RenderFromString_IfNonBool_Truthy()
        {
            var template = "$if(Name)Hello$endif";
            var model = new { Name = "Bob" };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Hello", result);
        }

        [TestMethod]
        public void RenderFromString_IfNonBool_Falsy()
        {
            var template = "$if(Name)Hello$endif";
            var model = new { Name = (string)null };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("", result);
        }

        #endregion

        #region RenderFromString — $foreach

        [TestMethod]
        public void RenderFromString_ForeachOverList_RendersEachItem()
        {
            var template = "$foreach(var item in Items)Item: ${item}$endfor";
            var model = new { Items = new List<string> { "A", "B" } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Item: AItem: B", result);
        }

        [TestMethod]
        public void RenderFromString_ForeachOverEmptyList_EmptyResult()
        {
            var template = "$foreach(var item in Items)X$endfor";
            var model = new { Items = new List<string>() };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_ForeachWithNestedProperty()
        {
            var template = "$foreach(var user in Users)Name: ${user.Name}$endfor";
            var model = new
            {
                Users = new[]
                {
                    new { Name = "John" },
                    new { Name = "Jane" }
                }
            };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Name: JohnName: Jane", result);
        }

        #endregion

        #region RenderFromString — Nesting & Complex

        [TestMethod]
        public void RenderFromString_IfInsideForeach()
        {
            var template = "$foreach(var user in Users)$if(user.IsActive)${user.Name}$endif$endfor";
            var model = new
            {
                Users = new[]
                {
                    new { Name = "John", IsActive = true },
                    new { Name = "Jane", IsActive = false }
                }
            };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("John", result);
        }

        [TestMethod]
        public void RenderFromString_ForeachInsideIf_True()
        {
            var template = "$if(ShowList)$foreach(var i in Numbers)${i}$endfor$endif";
            var model = new { ShowList = true, Numbers = new[] { 1, 2 } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("12", result);
        }

        [TestMethod]
        public void RenderFromString_ForeachInsideIf_False()
        {
            var template = "$if(ShowList)$foreach(var i in Numbers)${i}$endfor$endif";
            var model = new { ShowList = false, Numbers = new[] { 1, 2 } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_VariableInsideForeach()
        {
            var template = "Total: ${Count}$foreach(var x in Items)${x}$endfor";
            var model = new { Count = 3, Items = new[] { "X", "Y" } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Total: 3XY", result);
        }

        #endregion

        #region RenderFromFile / RenderToFile

        [TestMethod]
        public void RenderFromFile_FileExists_RendersCorrectly()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "<p>Hello, ${Name}!</p>");
            var model = new { Name = "World" };

            try
            {
                var result = _renderer.RenderFromFile(tempFile, model);
                Assert.AreEqual("<p>Hello, World!</p>", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void RenderFromFile_FileNotFound_Throws()
        {
            Assert.ThrowsException<FileNotFoundException>(() =>
                _renderer.RenderFromFile("nonexistent.tmpl", new { }));
        }

        [TestMethod]
        public void RenderToFile_WritesToFile()
        {
            var input = Path.GetTempFileName();
            var output = Path.GetTempFileName();
            File.WriteAllText(input, "Hi, ${User}!");
            var model = new { User = "Tester" };

            try
            {
                _renderer.RenderToFile(input, output, model);
                var content = File.ReadAllText(output);
                Assert.AreEqual("Hi, Tester!", content);
            }
            finally
            {
                File.Delete(input);
                File.Delete(output);
            }
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void RenderFromString_EmptyTemplate_ReturnsEmpty()
        {
            var result = _renderer.RenderFromString("", new { });
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_NullTemplate_ReturnsEmpty()
        {
            var result = _renderer.RenderFromString(null, new { });
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_NestedIfBlocks()
        {
            var template = "$if(A)$if(B)Inner$endif$endif";
            var model = new { A = true, B = true };
            var result = _renderer.RenderFromString(template, model);
            Assert.AreEqual("Inner", result);
        }

        [TestMethod]
        public void RenderFromString_MismatchedEndTag_ReturnsAsIs()
        {
            var template = "$if(Test)Content";
            var model = new { Test = true };
            var result = _renderer.RenderFromString(template, model);
            Assert.AreEqual(template, result);
        }

        [TestMethod]
        public void RenderFromString_ForeachWithInvalidSyntax_ReturnsAsIs()
        {
            var template = "$foreach(var in BadSyntax)X$endfor";
            var model = new { };
            var result = _renderer.RenderFromString(template, model);
            Assert.AreEqual(template, result);
        }


        [TestMethod]
        public void RenderFromString_ComplexTemplate_NestedIfForeachAndVariables()
        {
            var template = @"
        <html>
        <body>
            <h1>Welcome, ${User.Name}!</h1>
            $if(User.IsPremium)
                <p>You are a premium user!</p>
                <ul>
                $foreach(var product in User.PremiumProducts)
                    <li>${product.Name} ($${product.Price})</li>
                $endfor
                </ul>
            $else$
                <p>Upgrade to premium to see exclusive products.</p>
            $endif
            <footer>Total products shown: ${ProductCount}</footer>
        </body>
        </html>";

            var model = new
            {
                User = new
                {
                    Name = "Alex",
                    IsPremium = true,
                    PremiumProducts = new[]
                    {
                new { Name = "Book", Price = 19.99 },
                new { Name = "Course", Price = 99.99 }
            }
                },
                ProductCount = 2
            };

            var expected = @"
        <html>
        <body>
            <h1>Welcome, Alex!</h1>
                <p>You are a premium user!</p>
                <ul>
                    <li>Book ($19,99)</li>
                    <li>Course ($99,99)</li>
                </ul>
            <footer>Total products shown: 2</footer>
        </body>
        </html>";

            var result = _renderer.RenderFromString(template, model);

            // Убираем лишние переносы и пробелы для корректного сравнения
            string Normalize(string s) => Regex.Replace(s, @"\s+", " ").Trim();

            var normalizedExpected = Normalize(expected);
            var normalizedActual = Normalize(result);

            Console.WriteLine("Ожидаемое (нормализованное): " + normalizedExpected);
            Console.WriteLine("Фактическое (нормализованное): " + normalizedActual);

            Assert.AreEqual(normalizedExpected, normalizedActual);
        }
    }
    #endregion
}