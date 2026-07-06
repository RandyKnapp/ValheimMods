# frozen_string_literal: true

require "json"

###
# This script can clean out localization keys from non-English localization files
# this can be used to reset or update existing translations
# keys_to_remove below should be set to the key entries you want removed, if a key does not exist it is skipped
# this script can be ran by `ruby reset_select_language_files.rb`, it does not have any dependencies
#

language_files = Dir["localizations/*"]
keys_to_remove = %w[item_meteor_atgeir]

language_files.each do |lang_file|
  next if lang_file == "localizations/English.json"

  lang_json = JSON.parse(File.read("#{lang_file}"))
  puts "Removing keys from #{lang_file}"
  keys_to_remove.each do |rm_key|
    lang_json.delete(rm_key)
  end
  File.open("#{lang_file}", "w") { |f| f.write(JSON.pretty_generate(lang_json)) }
end
