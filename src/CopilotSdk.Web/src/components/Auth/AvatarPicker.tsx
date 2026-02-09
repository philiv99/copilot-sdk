/**
 * Avatar picker component for selecting preset or uploading custom avatars.
 */
import React, { useState, useRef, useCallback } from 'react';
import { AvatarPresetItem } from '../../types/user.types';
import { UserAvatar } from './UserAvatar';
import './AvatarPicker.css';

interface AvatarPickerProps {
  /** Currently selected avatar type. */
  selectedType: string;
  /** Currently selected avatar data. */
  selectedData?: string | null;
  /** Callback when avatar selection changes. */
  onChange: (avatarType: string, avatarData?: string | null) => void;
}

/** Built-in preset avatars. */
const defaultPresets: AvatarPresetItem[] = [
  { name: 'default', label: 'Default', emoji: '👤' },
  { name: 'astronaut', label: 'Astronaut', emoji: '🧑‍🚀' },
  { name: 'robot', label: 'Robot', emoji: '🤖' },
  { name: 'ninja', label: 'Ninja', emoji: '🥷' },
  { name: 'wizard', label: 'Wizard', emoji: '🧙' },
  { name: 'pirate', label: 'Pirate', emoji: '🏴‍☠️' },
  { name: 'alien', label: 'Alien', emoji: '👽' },
  { name: 'cat', label: 'Cat', emoji: '🐱' },
  { name: 'dog', label: 'Dog', emoji: '🐶' },
  { name: 'dragon', label: 'Dragon', emoji: '🐉' },
  { name: 'unicorn', label: 'Unicorn', emoji: '🦄' },
  { name: 'phoenix', label: 'Phoenix', emoji: '🔥' },
];

const MAX_FILE_SIZE = 256 * 1024; // 256KB

export function AvatarPicker({ selectedType, selectedData, onChange }: AvatarPickerProps) {
  const [uploadError, setUploadError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handlePresetClick = useCallback((preset: AvatarPresetItem) => {
    setUploadError(null);
    if (preset.name === 'default') {
      onChange('Default', null);
    } else {
      onChange('Preset', preset.name);
    }
  }, [onChange]);

  const handleUploadClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  const handleFileChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadError(null);

    if (!['image/jpeg', 'image/png'].includes(file.type)) {
      setUploadError('Only JPEG and PNG images are allowed.');
      return;
    }

    if (file.size > MAX_FILE_SIZE) {
      setUploadError('Image must be smaller than 256KB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const base64 = reader.result as string;
      onChange('Custom', base64);
    };
    reader.onerror = () => {
      setUploadError('Failed to read image file.');
    };
    reader.readAsDataURL(file);

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }, [onChange]);

  return (
    <div className="avatar-picker" data-testid="avatar-picker">
      <label className="avatar-picker__label">Choose Avatar</label>

      {/* Current selection preview */}
      <div className="avatar-picker__preview">
        <UserAvatar
          avatarType={selectedType as any}
          avatarData={selectedData}
          size="large"
        />
      </div>

      {/* Preset grid */}
      <div className="avatar-picker__presets" role="radiogroup" aria-label="Preset avatars">
        {defaultPresets.map((preset) => (
          <button
            key={preset.name}
            type="button"
            className={`avatar-picker__preset ${
              (selectedType === 'Preset' && selectedData === preset.name) ||
              (selectedType === 'Default' && preset.name === 'default')
                ? 'avatar-picker__preset--selected'
                : ''
            }`}
            onClick={() => handlePresetClick(preset)}
            title={preset.label}
            aria-label={`Select ${preset.label} avatar`}
            role="radio"
            aria-checked={
              (selectedType === 'Preset' && selectedData === preset.name) ||
              (selectedType === 'Default' && preset.name === 'default')
            }
            data-testid={`preset-${preset.name}`}
          >
            <span role="img" aria-hidden="true">{preset.emoji}</span>
          </button>
        ))}
      </div>

      {/* Upload button */}
      <div className="avatar-picker__upload">
        <button
          type="button"
          className="avatar-picker__upload-btn"
          onClick={handleUploadClick}
          data-testid="upload-avatar-btn"
        >
          📷 Upload Custom Image
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png"
          onChange={handleFileChange}
          className="avatar-picker__file-input"
          data-testid="avatar-file-input"
          aria-label="Upload avatar image"
        />
        <span className="avatar-picker__upload-hint">JPEG or PNG, max 256KB</span>
      </div>

      {uploadError && (
        <p className="avatar-picker__error" role="alert" data-testid="avatar-upload-error">
          {uploadError}
        </p>
      )}
    </div>
  );
}
