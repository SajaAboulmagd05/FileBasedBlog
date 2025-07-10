A month ago I set out to build a free open-source PDF chat app. I honestly expected it to be super easy.

Upload a PDF, extract the text, feed it to an LLM. What could go wrong?

As it turned out, PDF parsing proved extremely annoying. I tried PyMuPDF, Unstructured, Chunkr, and every other PDF parsing library I could find. They were either painfully slow, prohibitively expensive, or produced garbage output. Server-side processing meant users waited 20+ seconds watching a spinner. Adding PDF processing to my backend added a lot of complexity. Worst of all, as a user, I didn’t love the experience.

I wrote an article about my journey building pdfgpt.co, but have since come up with a better solution: a little known library called @opendocsg/pdf2md.

The Enterprise PDF Processing Disaster Nobody Talks About
While debugging my app, I started talking to enterprise engineering teams about their PDF processing. The scale they were working at genuinely surprised me. One consumer chat app with millions of active users processes tens of millions of PDF pages a month. For another it’s in the range of hundreds of millions of pages.

The infrastructure always looks the same: a microservice with PyMuPDF or similar library that’s constantly needing maintenance, and costs tens of thousands per month. As the microservice grows, it needs load balancers for traffic distribution, queue systems for spikes, and a DevOps constantly handling fires.

When you account for time wasted, the cost easily expands to hundreds of thousands per year, just for processing PDFs at a large scale.

And it all starts from some innocuous code like this:

# Upload large pdf file to server (network latency)
pdf_content = await file.read()
# Process on server (extremely CPU intensive)
markdown = extract_text_from_pdf(pdf_content)
# Return result (more network latency)
return {"markdown": markdown}
My solution looked a lot like the above — the exact pdf parsing lib isn’t as important as the fact that my best-case pdf processing time was still several seconds. I didn’t even have a separate microservice for PDF parsing — the stability of my project was in serious danger at any real scale.

Then I found @opendocsg/pdf2md — a JavaScript library that runs entirely in the browser. No servers. No uploads. No infrastructure.

The Solution: Let the Browser Do the Work
Here’s the beautiful thing about modern browsers — they’re incredibly powerful. We’re talking about runtime environments that can handle 3D graphics, compile WebAssembly, and process gigabytes of data. So why are we uploading PDFs to servers for basic text extraction? With @opendocsg/pdf2md, the entire process happens client-side: