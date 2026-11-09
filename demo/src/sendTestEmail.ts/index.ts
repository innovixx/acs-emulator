import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { EmailSendResponse } from '@azure/communication-email';
import { EmailClient } from '@azure/communication-email';
import dotenv from 'dotenv';
dotenv.config();

export const sendTestEmail = async () => {
	const client = new EmailClient(`${process.env.AZURE_COMMUNICATION_CONNECTION_STRING}`, {
		allowInsecureConnection: true,
	});

	const poller = await client.beginSend({
		content: {
			html: "<p>Hello</p>",
			plainText: "Hello",
			subject: "Test",
		},
		recipients: {
			to: [{ address: "test@example.com" }],
		},
		senderAddress: "test@example.com",
	});

	const result = await poller.pollUntilDone();

	console.log(JSON.stringify(result, null, 2));
};

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
	sendTestEmail().catch((error: unknown) => {
		console.error(error);
		process.exitCode = 1;
	});
}
