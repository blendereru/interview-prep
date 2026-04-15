using Delegates_Concepts;
using Delegates_Concepts.Implementations;

var fileHandler = new FileHandler();
var files = new List<SystemFile>
{
    new PdfFile 
    { 
        Name = "Report1", 
        Content = "PDF: Annual report data..." 
    },

    new DocsFile 
    { 
        Name = "Resume", 
        Content = "DOCS: John Doe Resume..." 
    },

    new TxtFile 
    { 
        Name = "Notes", 
        Content = "TXT: Quick notes from meeting..." 
    },

    new PdfFile 
    { 
        Name = "Invoice", 
        Content = "PDF: Invoice #12345" 
    },

    new DocsFile 
    { 
        Name = "ProjectPlan", 
        Content = "DOCS: Project milestones..." 
    },

    new TxtFile 
    { 
        Name = "Log", 
        Content = "TXT: System logs..." 
    }
};

fileHandler.Upload(files);

Console.WriteLine("Displaying files...");
fileHandler.DisplayFiles(file => file.Extension == "pdf");