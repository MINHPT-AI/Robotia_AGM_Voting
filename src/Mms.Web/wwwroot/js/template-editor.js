/**
 * Template Editor JS Interop
 * - Mammoth.js: DOCX → HTML conversion (client-side)
 * - TinyMCE: WYSIWYG editor with custom token toolbar
 */
window.templateEditor = {

    // ── Convert DOCX bytes to HTML using Mammoth.js ──
    convertDocxToHtml: async function (bytes) {
        const arrayBuffer = new Uint8Array(bytes).buffer;
        const result = await mammoth.convertToHtml({ arrayBuffer: arrayBuffer });
        return result.value;
    },

    // ── Initialize TinyMCE editor ──
    initEditor: function (elementId, tokens, signatureUrl, sealUrl, dotNetRef, margins, legalRepTitle, legalRepName) {
        // Destroy previous instance if exists
        if (tinymce.get(elementId)) {
            tinymce.get(elementId).destroy();
        }

        // Apply margins passed from server
        let marginCss = '2cm 2cm 3cm 2cm'; // default
        if (margins) {
            marginCss = `${margins.top}cm ${margins.right}cm ${margins.bottom}cm ${margins.left}cm`;
        }

        // Fallbacks for signature block
        legalRepTitle = legalRepTitle || 'Chủ tịch HĐQT';
        legalRepName = legalRepName || 'Họ và tên';

        tinymce.init({
            selector: '#' + elementId,
            height: 650,
            language: 'vi',
            base_url: 'https://cdn.jsdelivr.net/npm/tinymce@7',
            license_key: 'gpl',
            plugins: 'image table lists code fullscreen searchreplace pagebreak wordcount visualblocks preview',
            
            font_size_formats: '9pt 10pt 11pt 12pt 13pt 13.5pt 14pt 16pt 18pt 20pt 24pt',
            line_height_formats: '1.0 1.15 1.2 1.3 1.4 1.5 1.6 1.8 2.0',
            font_family_formats: "Times New Roman=Times New Roman,serif; Arial=Arial,sans-serif; Calibri=Calibri,sans-serif;",
            
            toolbar: [
                'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | subscript superscript',
                'forecolor backcolor | alignleft aligncenter alignright alignjustify | lineheight | indent outdent | paraformat | insertdecoline',
                'bullist numlist | table image | pagebreak | inserttoken insertsignseal | preview fullscreen code wordcount'
            ],
            menubar: 'file edit view insert format table',
            
            style_formats: [
                { title: 'Giãn dòng 1.2 (VB dài)', block: 'p', styles: { 'line-height': '1.2' } },
                { title: 'Giãn dòng 1.4 (VB ngắn)', block: 'p', styles: { 'line-height': '1.4' } },
                { title: 'Thụt đầu dòng 1cm', block: 'p', styles: { 'text-indent': '1cm' } },
                { title: 'Paragraph spacing 6pt', block: 'p', styles: { 'margin-bottom': '6pt' } },
                { title: 'Paragraph spacing 12pt', block: 'p', styles: { 'margin-bottom': '12pt' } },
            ],

            content_style: `
                body { font-family: 'Times New Roman', serif; font-size: 13pt; line-height: 1.4; margin: ${marginCss}; }
                .mce-token {
                    background: #e3f2fd; border: 1px solid #90caf9; border-radius: 4px;
                    padding: 1px 6px; font-weight: 600; color: #1565c0; white-space: nowrap;
                    cursor: default; user-select: none;
                }
                .sign-seal-block {
                    text-align: center;
                    display: inline-block;
                    min-width: 300px;
                }
                img.seal-stamp { max-width: 150px; }
                img.signature-img { max-width: 180px; }
                hr.deco-line-short {
                    width: 5cm;
                    margin: 2px auto;
                    border: none;
                    border-top: 1px solid #000;
                }
                hr.deco-line-long {
                    width: 8cm;
                    margin: 2px auto;
                    border: none;
                    border-top: 1px solid #000;
                }
            `,
            // Prevent TinyMCE from stripping our custom elements
            valid_elements: '*[*]',
            extended_valid_elements: 'span[class|data-token|contenteditable|style],hr[class|style]',
            // Image upload (for drag-drop or paste)
            automatic_uploads: false,

            setup: function (editor) {
                // ── Token menu button ──
                editor.ui.registry.addMenuButton('inserttoken', {
                    text: 'Chèn Token',
                    icon: 'bookmark',
                    fetch: function (callback) {
                        var items = tokens.map(function (t) {
                            return {
                                type: 'menuitem',
                                text: t.code + ' — ' + t.description,
                                onAction: function () {
                                    editor.insertContent(
                                        '<span class="mce-token" data-token="' + t.code + '" contenteditable="false">' +
                                        t.code + '</span>&nbsp;'
                                    );
                                }
                            };
                        });
                        callback(items);
                    }
                });

                // ── Signature & Seal combo button ──
                editor.ui.registry.addButton('insertsignseal', {
                    text: 'Ký & Dấu',
                    icon: 'edit-image',
                    enabled: !!signatureUrl || !!sealUrl,
                    onAction: function () {
                        let signHtml = signatureUrl ? `<img src="${signatureUrl}" alt="Chữ ký" style="position:relative; z-index:1; max-width:180px;" />` : '';
                        let sealHtml = sealUrl ? `<img src="${sealUrl}" alt="Con dấu" style="position:absolute; top:-20px; left:-60px; z-index:2; max-width:140px; opacity:0.85; pointer-events:none;" />` : '';
                        
                        let html = `
                        <div style="text-align: right; margin-top: 24pt;">
                            <div class="sign-seal-block" style="position:relative; display:inline-block; text-align:center; min-width:300px;">
                                <p style="font-weight:bold; text-transform:uppercase; margin-bottom:4px;">T/M. HỘI ĐỒNG QUẢN TRỊ</p>
                                <p style="font-weight:bold; margin-bottom:4px;">${legalRepTitle}</p>
                                <p style="font-style:italic; margin-bottom:40px;">(Ký, đóng dấu)</p>
                                <div style="position:relative; display:inline-block;">
                                    ${signHtml}
                                    ${sealHtml}
                                </div>
                                <p style="font-weight:bold; margin-top:4px;">${legalRepName}</p>
                            </div>
                        </div>
                        <p>&nbsp;</p>`;
                        
                        editor.insertContent(html);
                    }
                });

                // ── Decorative Line dropdown ──
                editor.ui.registry.addMenuButton('insertdecoline', {
                    text: '━',
                    tooltip: 'Gạch ngang tiêu ngữ',
                    fetch: function(callback) {
                        callback([
                            {
                                type: 'menuitem',
                                text: 'Gạch ngắn (Cơ quan)',
                                onAction: function() {
                                    editor.insertContent('<hr class="deco-line-short" />');
                                }
                            },
                            {
                                type: 'menuitem',
                                text: 'Gạch dài (Tiêu ngữ)',
                                onAction: function() {
                                    editor.insertContent('<hr class="deco-line-long" />');
                                }
                            }
                        ]);
                    }
                });

                // ── Paragraph Formatting Dialog ──
                editor.ui.registry.addButton('paraformat', {
                    icon: 'paragraph',
                    tooltip: 'Định dạng đoạn văn',
                    onAction: function () {
                        editor.windowManager.open({
                            title: 'Định dạng đoạn (Paragraph)',
                            body: {
                                type: 'panel',
                                items: [
                                    { type: 'input', name: 'indent', label: 'Thụt dòng đầu (cm) - VD: 1', placeholder: 'Ví dụ: 1' },
                                    { type: 'input', name: 'spaceBefore', label: 'Cách đoạn trên (pt)', placeholder: 'Ví dụ: 0' },
                                    { type: 'input', name: 'spaceAfter', label: 'Cách đoạn dưới (pt)', placeholder: 'Ví dụ: 6' }
                                ]
                            },
                            buttons: [
                                { type: 'cancel', text: 'Hủy' },
                                { type: 'submit', text: 'Áp dụng', primary: true }
                            ],
                            onSubmit: function (api) {
                                var data = api.getData();
                                var styles = {};
                                if (data.indent) styles['text-indent'] = data.indent + (data.indent.includes('c') ? '' : 'cm');
                                if (data.spaceBefore) styles['margin-top'] = data.spaceBefore + (data.spaceBefore.includes('p') ? '' : 'pt');
                                if (data.spaceAfter) styles['margin-bottom'] = data.spaceAfter + (data.spaceAfter.includes('p') ? '' : 'pt');
                                
                                editor.formatter.register('custom_para', {
                                    block: 'p',
                                    styles: styles
                                });
                                editor.formatter.apply('custom_para');
                                api.close();
                            }
                        });
                    }
                });
            }
        });
    },

    // ── Get HTML content from editor ──
    getContent: function (elementId) {
        var editor = tinymce.get(elementId);
        if (editor) {
            return editor.getContent();
        } else {
            var el = document.getElementById(elementId);
            return el ? el.value : '';
        }
    },

    // ── Set HTML content in editor ──
    setContent: function (elementId, html) {
        var editor = tinymce.get(elementId);
        if (editor) {
            editor.setContent(html);
        }
    },

    // ── Destroy editor instance ──
    destroy: function (elementId) {
        var editor = tinymce.get(elementId);
        if (editor) {
            editor.destroy();
        }
    }
};

