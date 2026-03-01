const fs = require('fs');
const text = fs.readFileSync('bin/Debug/net8.0-windows/onstage_dump.txt', 'utf8');

console.log('Total characters:', text.length);

// Because OnStage uses iframes widely, let's see if we can find any iframe contents
const iframesMatches = text.match(/--- IFRAME [\s\S]*?(?=--- IFRAME|$)/g);
if (iframesMatches) {
    console.log(`Found ${iframesMatches.length} iframe blocks.`);
} else {
    console.log('No iframe blocks formatted with --- IFRAME found.');
}

// Extract table rows from the whole text
const rows = text.match(/<tr[^>]*>.*?<\/tr>/gi);
if (rows && rows.length > 0) {
    console.log(`Found ${rows.length} TR elements.`);
    let out = '';
    for(let i=0; i<Math.min(20, rows.length); i++) {
        // clean up html
        let rowText = rows[i].replace(/<[^>]+>/g, '|').replace(/\|+/g, '|').trim();
        out += `Row ${i}: ${rowText}\n`;
    }
    fs.writeFileSync('rows_dump.txt', out);
    console.log('Wrote top 20 rows to rows_dump.txt');
} else {
    console.log('No TR elements found.');
    // Check for divs with "row" or "list-item" or "mdc-data-table__row"
    const divs = text.match(/<div[^>]*class=\"[^\"]*(row|mdc-data-table__row|list-item|card)[^\"]*\"[^>]*>.*?<\/div>/gi);
    if (divs && divs.length > 0) {
        console.log(`Found ${divs.length} div-based rows.`);
        let out = '';
        for(let i=0; i<Math.min(20, divs.length); i++) {
            let divText = divs[i].replace(/<[^>]+>/g, '|').replace(/\|+/g, '|').trim();
            out += `Div Row ${i}: ${divText}\n`;
        }
        fs.writeFileSync('rows_dump.txt', out);
        console.log('Wrote top 20 div-rows to rows_dump.txt');
    }
}
