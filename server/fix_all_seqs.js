const postgres = require('postgres');

const sql = postgres('postgres://postgres:eu+FjA%40Szv%2CyK9b@db.gnsjkrxmxrentvrnqaww.supabase.co:5432/postgres', {
  ssl: 'require'
});

async function main() {
  const tablesWithId = [
    'category',
    'customer_tier',
    'customer_voucher',
    'order',
    'order_detail',
    'order_tax',
    'order_voucher',
    'owner',
    'product',
    'tax',
    'voucher'
  ];

  try {
    for (const table of tablesWithId) {
      console.log(`Checking table: ${table}...`);
      try {
        // Find the sequence name associated with the id column
        const seqInfo = await sql`
          SELECT pg_get_serial_sequence(${table}, 'id') as seq_name;
        `;
        
        const seqName = seqInfo[0]?.seq_name;

        if (seqName) {
          // Reset the sequence to the maximum id in the table
          await sql`
            SELECT setval(${seqName}, COALESCE((SELECT MAX(id) FROM ${sql(table)}), 1));
          `;
          console.log(`✅ Fixed sequence for ${table} (${seqName})`);
        } else {
          console.log(`⚠️ No auto-increment sequence found for ${table}.`);
        }
      } catch (err) {
        console.log(`❌ Error processing ${table}: ${err.message}`);
      }
    }
  } catch (err) {
    console.error('Fatal error:', err);
  } finally {
    await sql.end();
  }
}

main();
