import { Link, Panel, PanelType, Pivot, PivotItem } from '@fluentui/react';
import { EmailMessage } from '../services/email';

interface EmailPreviewProps {
  email: EmailMessage;
  onDismiss: () => void;
}

export const EmailPreview = ({ email, onDismiss }: EmailPreviewProps) => {
  const headers = [
    { label: 'Operation Id', value: email.operationId },
    { label: 'From', value: email.from },
    { label: 'To', value: email.to },
    { label: 'Cc', value: email.cc },
    { label: 'Bcc', value: email.bcc },
    { label: 'Reply To', value: email.replyTo },
    { label: 'Disable Tracking', value: String(email.disableUserEngagementTracking) }
  ];

  return (
    <Panel
      isOpen
      type={PanelType.large}
      headerText="Email preview"
      closeButtonAriaLabel="Close email preview"
      onDismiss={onDismiss}
    >
      <h2 style={{ overflowWrap: 'anywhere' }}>{email.subject || '(No subject)'}</h2>
      <dl style={{ display: 'grid', gridTemplateColumns: 'max-content minmax(0, 1fr)', gap: '8px 16px' }}>
        {headers.filter(header => header.value).map(header => (
          <div key={header.label} style={{ display: 'contents' }}>
            <dt style={{ fontWeight: 600 }}>{header.label}</dt>
            <dd style={{ margin: 0, whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{header.value}</dd>
          </div>
        ))}
        {email.attachments.length > 0 && (
          <div style={{ display: 'contents' }}>
            <dt style={{ fontWeight: 600 }}>Attachments</dt>
            <dd style={{ margin: 0, overflowWrap: 'anywhere' }}>
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {email.attachments.map(attachment => (
                  <li key={attachment.id}>
                    <Link
                      href={`data:application/octet-stream;base64,${attachment.contentBytesBase64}`}
                      download={attachment.name}
                      aria-label={`Download ${attachment.name}`}
                    >
                      {attachment.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </dd>
          </div>
        )}
      </dl>
      <Pivot aria-label="Email content" defaultSelectedKey={email.html ? 'html' : 'plainText'}>
        <PivotItem headerText="HTML" itemKey="html">
          {email.html ? (
            <iframe
              title="Email HTML preview"
              sandbox=""
              referrerPolicy="no-referrer"
              srcDoc={`<!doctype html><html><head><meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'"></head><body>${email.html}</body></html>`}
              style={{ width: '100%', height: '60vh', border: '1px solid #edebe9', marginTop: 16, background: '#fff' }}
            />
          ) : <p>This email has no HTML body.</p>}
        </PivotItem>
        <PivotItem headerText="Plain text" itemKey="plainText">
          {email.plainText ? (
            <pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{email.plainText}</pre>
          ) : <p>This email has no plain-text body.</p>}
        </PivotItem>
        <PivotItem headerText="HTML source" itemKey="source">
          {email.html ? (
            <pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{email.html}</pre>
          ) : <p>This email has no HTML body.</p>}
        </PivotItem>
      </Pivot>
    </Panel>
  );
};
